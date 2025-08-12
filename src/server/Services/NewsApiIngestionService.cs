using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using talking_points.Models;

namespace talking_points.server.Ingestion
{
	public class NewsApiIngestionService
	{
		private readonly HttpClient _httpClient;
		private const string BaseUrl = "https://newsapi.org/v2/top-headlines";
		private readonly IConfiguration _config;

		public NewsApiIngestionService(HttpClient httpClient, IConfiguration config)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_config = config;
		}

		public async Task<IEnumerable<ArticleDetails>> FetchTopHeadlinesAsync(string country = "us", int pageSize = 100)
		{
			var articles = new List<ArticleDetails>();
			int page = 1;
			int totalResults = 0;
			var apiKey = _config["NewsAPIKey"]; // support either key name
												// Ensure headers present (idempotent)
			if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
			{
				_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"talking-points-app/1.0 (+https://github.com/bharney/talking-points)");
			}
			if (!string.IsNullOrEmpty(apiKey) && !_httpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
			{
				_httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
			}
			do
			{
				var url = $"{BaseUrl}?country={country}&pageSize={pageSize}&page={page}"; // API key supplied via header
				var response = await _httpClient.GetAsync(url);
				var json = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode)
				{
					var errorMsg = $"NewsAPI request failed. URL: {url}\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\nResponse: {json}";
					throw new Exception(errorMsg);
				}
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("articles", out var articlesElement))
				{
					foreach (var article in articlesElement.EnumerateArray())
					{
						var sourceId = article.GetProperty("source").TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
						var sourceName = article.GetProperty("source").TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
						articles.Add(new ArticleDetails
						{
							Id = Guid.NewGuid(), // Generate a new GUID for each article
							Source = sourceId ?? string.Empty,
							SourceName = sourceName ?? string.Empty,
							Author = article.TryGetProperty("author", out var authorProp) ? (authorProp.GetString() ?? string.Empty) : string.Empty,
							Title = article.TryGetProperty("title", out var titleProp) ? (titleProp.GetString() ?? string.Empty) : string.Empty,
							Description = article.TryGetProperty("description", out var descProp) ? (descProp.GetString() ?? string.Empty) : string.Empty,
							URL = article.TryGetProperty("url", out var urlProp) ? (urlProp.GetString() ?? string.Empty) : string.Empty,
							UrlToImage = article.TryGetProperty("urlToImage", out var imgProp) ? (imgProp.GetString() ?? string.Empty) : string.Empty,
							PublishedAt = article.TryGetProperty("publishedAt", out var pubProp) && DateTime.TryParse(pubProp.GetString(), out var dt) ? dt : (DateTime?)null,
							Content = article.TryGetProperty("content", out var contentProp) ? (contentProp.GetString() ?? string.Empty) : string.Empty
						});
					}
				}
				if (doc.RootElement.TryGetProperty("totalResults", out var totalResultsElement))
				{
					totalResults = totalResultsElement.GetInt32();
				}
				page++;
			} while ((page - 1) * pageSize < totalResults);
			return articles;
		}
	}
}
