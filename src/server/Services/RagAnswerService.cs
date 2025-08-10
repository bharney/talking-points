using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using talking_points.Models;
using talking_points.Services.Caching;

namespace talking_points.Services
{
    public interface IRagAnswerService
    {
        Task<string> CreateAnswerAsync(string query, IReadOnlyList<NewsArticle> contextArticles);
    }

    public class RagAnswerService : IRagAnswerService
    {
        private readonly OpenAIClient _client;
        private readonly string _chatDeployment;
        private readonly ILogger<RagAnswerService> _logger;
        private readonly IAnswerCache? _answerCache;
        private readonly bool _enableCache;
        private readonly TimeSpan _ttl;
        public RagAnswerService(IConfiguration config, ILogger<RagAnswerService> logger, IAnswerCache? answerCache = null)
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
            _answerCache = answerCache;
            _enableCache = bool.TryParse(config["Cache:EnableAnswers"], out var ea) ? ea : true;
            _ttl = TimeSpan.FromMinutes(int.TryParse(config["Cache:AnswerTtlMinutes"], out var m) ? m : 30);
        }

        public async Task<string> CreateAnswerAsync(string query, IReadOnlyList<NewsArticle> contextArticles)
        {
            if (_enableCache && _answerCache != null)
            {
                try
                {
                    var cached = await _answerCache.GetAsync(query, contextArticles);
                    if (cached != null) return cached;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Answer cache get failed; continuing");
                }
            }

            var sb = new StringBuilder();
            for (int i = 0; i < contextArticles.Count; i++)
            {
                var a = contextArticles[i];
                var snippet = a.Content.Length > 800 ? a.Content.Substring(0, 800) : a.Content;
                sb.AppendLine($"[S{i+1}] Title: {a.Title}\nSnippet: {snippet}\n");
            }
            var systemPrompt = "You are a concise assistant. Use only the sources provided. Cite sources as [S#]. If unsure, say you do not know.";
            var userPrompt = $"Question: {query}\nSources:\n{sb}\nAnswer:";

            var chat = new ChatCompletionsOptions
            {
                Temperature = 0.2f,
                MaxTokens = 500,
                DeploymentName = _chatDeployment
            };
            chat.Messages.Add(new ChatRequestSystemMessage(systemPrompt));
            chat.Messages.Add(new ChatRequestUserMessage(userPrompt));
            var response = await _client.GetChatCompletionsAsync(chat);
            var answer = response.Value.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

            if (_enableCache && _answerCache != null && !string.IsNullOrWhiteSpace(answer))
            {
                try
                {
                    await _answerCache.SetAsync(query, contextArticles, answer, _ttl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Answer cache set failed");
                }
            }
            return answer;
        }
    }
}
