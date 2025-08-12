using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure;
using talking_points;
using talking_points.Models;
using talking_points.Repository;

namespace talking_points.Services
{

	public interface IKeywordService
	{
		Task<IEnumerable<Keywords>> GenerateKeywordsAsync(IEnumerable<ArticleDetails> articles);
	}

	public class KeywordService : IKeywordService
	{
		private readonly OpenAIClient _client;
		private readonly ILogger<KeywordService> _logger;
		private readonly string _chatDeployment;
		private readonly IKeywordRepository _keywordRepository;
		private readonly IArticleRepository _articleRepository;


		public KeywordService(IConfiguration config, ILogger<KeywordService> logger, IKeywordRepository keywordRepository, IArticleRepository articleRepository)
		{
			var endpoint = config["AzureOpenAIEndpoint"] ?? config["OpenAIEndpoint"];
			var key = config["AzureOpenAIKey"] ?? config["OpenAIKey"];
			_chatDeployment = config["AzureOpenAI:ChatDeployment"] ?? "gpt-4.1";
			if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException("Azure OpenAI endpoint/key missing from configuration.");
			}
			_client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
			_logger = logger;
			_keywordRepository = keywordRepository;
			_articleRepository = articleRepository;
		}

		// Generate keywords for ArticleDetails. We key the model output by articleId (GUID string) and persist
		// directly against the provided ArticleDetails. If mapping/parsing fails for an item, skip it and continue.
		public async Task<IEnumerable<Keywords>> GenerateKeywordsAsync(IEnumerable<ArticleDetails> articles)
		{
			var allKeywords = new List<Keywords>();
			// Build a single, batched prompt from all articles while keeping within a safe size
			// Limit each article to a reasonable number of words to avoid hitting token limits.
			static string TruncateWords(string text, int maxWords)
			{
				if (string.IsNullOrWhiteSpace(text)) return string.Empty;
				var parts = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length <= maxWords) return text;
				return string.Join(" ", parts.AsSpan(0, maxWords).ToArray());
			}

			const int perArticleWordLimit = 300;
			const int maxArticlesInBatch = 5; // avoid overly large prompts
			var inputs = new List<(Guid Id, string Title, string Body)>();
			foreach (var article in articles)
			{
				if (article.Id == Guid.Empty || string.IsNullOrWhiteSpace(article.URL)) continue;
				var title = article.Title ?? string.Empty;
				var body = TruncateWords(article.Content ?? article.Description ?? string.Empty, perArticleWordLimit);
				if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)) continue;
				inputs.Add((article.Id, title, body));
				if (inputs.Count >= maxArticlesInBatch) break;
			}

			// If nothing to process, short-circuit by returning empty result
			if (inputs.Count == 0)
			{
				return allKeywords;
			}

			// Build a JSON-in/JSON-out instruction so we can map keywords back to article IDs (GUID as string).
			var articlesJson = JsonSerializer.Serialize(inputs.Select(i => new { id = i.Id.ToString(), title = i.Title, body = i.Body }));
			var prompt =
				"You will be given an array of news articles in JSON with fields id (string GUID), title, and body. " +
				"For each article, extract 1-3 concise, relevant keywords (NYT-style). " +
				"Output requirements: return ONLY a compact JSON array (no prose, no markdown) with exactly one item per input article, " +
				"each item shaped as {\\\"articleId\\\": \\\"<guid>\\\", \\\"keywords\\\": [\\\"k1\\\", \\\"k2\\\", ...]}. " +
				"Keywords must be lowercase, 1-3 words each, and deduplicated. " +
				"If content is insufficient, return an empty keywords array for that article. " +
				"Produce strictly valid JSON with no trailing commas and minimal whitespace.\n\n" +
				$"articles: {articlesJson}";

			var chatCompletionsOptions = new ChatCompletionsOptions
			{
				MaxTokens = 512,
				Temperature = 0.3f,
				DeploymentName = _chatDeployment
			};
			chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage("You extract concise, relevant keywords and answer strictly in JSON."));
			chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(prompt));

			var completionResponse = await _client.GetChatCompletionsAsync(
				chatCompletionsOptions
			);

			var content = completionResponse.Value.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
			// Try parse JSON output
			List<ParsedItem> parsed = new();
			try
			{
				parsed = JsonSerializer.Deserialize<List<ParsedItem>>(ExtractJson(content)) ?? new();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to parse keywords JSON; content: {content}", content);
				return allKeywords; // terminate early for this batch
			}

			// Build a lookup of articleId (string GUID) -> ArticleDetails for persistence
			var byId = articles.Where(a => a != null)
				.ToDictionary(a => a.Id.ToString(), a => a);
			foreach (var item in parsed)
			{
				if (item == null || item.Keywords == null || item.Keywords.Count == 0) continue;
				var idValue = item.ArticleId ?? item.Id;
				if (string.IsNullOrWhiteSpace(idValue)) continue;
				if (!byId.TryGetValue(idValue, out var details)) continue; // can't map; skip
				var unique = item.Keywords
					.Where(s => !string.IsNullOrWhiteSpace(s))
					.Select(s => s.Trim())
					.Where(s => s.Length > 1)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				if (unique.Count == 0) continue;
				foreach (var kw in unique)
				{
					var entity = new Keywords
					{
						Id = Guid.NewGuid(),
						Keyword = kw,
						ArticleId = details.Id
					};
					await _keywordRepository.Insert(entity);
					allKeywords.Add(entity);
				}
			}

			return allKeywords;
		}

		private sealed class ParsedItem
		{
			// For ArticleDetails flow (GUID as string)
			public string? ArticleId { get; set; }
			public string? Id { get; set; } // tolerate either name
			public List<string>? Keywords { get; set; }
		}

		private static string ExtractJson(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return "[]";
			var start = text.IndexOf('[');
			var end = text.LastIndexOf(']');
			if (start >= 0 && end > start)
			{
				return text.Substring(start, end - start + 1);
			}
			return text.Trim();
		}
	}

}
