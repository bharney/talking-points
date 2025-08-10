using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

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

        public async Task<IEnumerable<talking_points.Models.NewsArticle>> FetchTopHeadlinesAsync(string country = "us", int pageSize = 100)
        {
            var articles = new List<talking_points.Models.NewsArticle>();
            int page = 1;
            int totalResults = 0;
            var apiKey = _config["NewsAPIKey"];
            do
            {
                var url = $"{BaseUrl}?country={country}&pageSize={pageSize}&page={page}&apiKey={apiKey}";
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
                        articles.Add(new talking_points.Models.NewsArticle
                        {
                            SourceId = sourceId ?? string.Empty,
                            SourceName = sourceName ?? string.Empty,
                            Author = article.TryGetProperty("author", out var authorProp) ? (authorProp.GetString() ?? string.Empty) : string.Empty,
                            Title = article.TryGetProperty("title", out var titleProp) ? (titleProp.GetString() ?? string.Empty) : string.Empty,
                            Description = article.TryGetProperty("description", out var descProp) ? (descProp.GetString() ?? string.Empty) : string.Empty,
                            Url = article.TryGetProperty("url", out var urlProp) ? (urlProp.GetString() ?? string.Empty) : string.Empty,
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
