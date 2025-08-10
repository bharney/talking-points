using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using talking_points.Models;

namespace talking_points.Services
{
    public interface IVectorIndexService
    {
        Task EnsureIndexAsync();
        Task UpsertArticlesAsync(IEnumerable<NewsArticle> articles);
        Task<IReadOnlyList<(NewsArticle Article, double Score)>> HybridSearchAsync(string query, int top = 10);
    }

    public class VectorIndexService : IVectorIndexService
    {
        private readonly string _indexName;
        private readonly SearchIndexClient _indexClient;
        private readonly SearchClient _searchClient;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<VectorIndexService> _logger;

        public VectorIndexService(IConfiguration config,
                                   IEmbeddingService embeddingService,
                                   ILogger<VectorIndexService> logger,
                                   SearchIndexClient indexClient,
                                   SearchClient searchClient)
        {
            _indexName = config["AzureSearch:IndexName"] ?? "news-articles";
            _embeddingService = embeddingService;
            _logger = logger;
            _indexClient = indexClient;
            _searchClient = searchClient;
        }

        public async Task EnsureIndexAsync()
        {
            try
            {
                var exists = false;
                await foreach (var name in _indexClient.GetIndexNamesAsync())
                {
                    if (name == _indexName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) return;
            }
            catch { /* continue to create */ }

            var vectorDimensions = 1536; // embedding model dims
            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("title") { IsFilterable = true },
                new SearchableField("description"),
                new SearchableField("content"),
                new SimpleField("publishedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                // Keep vector field (some SDKs allow specifying dimensions even if vector config object not exposed)
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = vectorDimensions
                }
            };
            var definition = new SearchIndex(_indexName, fields);
            await _indexClient.CreateOrUpdateIndexAsync(definition);
        }

        public async Task UpsertArticlesAsync(IEnumerable<NewsArticle> articles)
        {
            var batch = new List<IndexDocumentsAction<SearchDocument>>();
            foreach (var a in articles)
            {
                var text = string.Join("\n", new[]{ a.Title, a.Description ?? string.Empty, a.Content });
                var embedding = await _embeddingService.EmbedAsync(text);
                var doc = new SearchDocument
                {
                    ["id"] = a.Id.ToString(),
                    ["title"] = a.Title,
                    ["description"] = a.Description ?? string.Empty,
                    ["content"] = a.Content,
                    ["publishedAt"] = a.PublishedAt,
                    ["embedding"] = embedding
                };
                batch.Add(IndexDocumentsAction.MergeOrUpload(doc));
            }
            if (batch.Count > 0)
            {
                var result = await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Create<SearchDocument>(batch.ToArray()));
                if (result.Value.Results.Any(r => r.Succeeded == false))
                {
                    _logger.LogWarning("Some documents failed to index: {fails}", string.Join(",", result.Value.Results.Where(r => !r.Succeeded).Select(r => r.Key)));
                }
            }
        }

        public async Task<IReadOnlyList<(NewsArticle Article, double Score)>> HybridSearchAsync(string query, int top = 10)
        {
            // Embed query and perform lexical search, then cosine re-rank if embeddings present.
            var queryEmbedding = await _embeddingService.EmbedAsync(query);
            var options = new SearchOptions { Size = top, IncludeTotalCount = true };
            options.Select.Add("id");
            options.Select.Add("title");
            options.Select.Add("description");
            options.Select.Add("content");
            options.Select.Add("publishedAt");
            options.Select.Add("embedding");
            var results = await _searchClient.SearchAsync<SearchDocument>(query, options);
            var provisional = new List<(NewsArticle Article, double Score, float[]? Embedding)>();
            await foreach (var r in results.Value.GetResultsAsync())
            {
                var doc = r.Document;
                float[]? emb = null;
                if (doc.TryGetValue("embedding", out var embObj) && embObj is IEnumerable<float> floats)
                {
                    emb = floats.ToArray();
                }
                provisional.Add((new NewsArticle
                {
                    Id = int.TryParse(doc["id"].ToString(), out var idVal) ? idVal : 0,
                    Title = doc.ContainsKey("title") ? doc["title"]?.ToString() ?? string.Empty : string.Empty,
                    Description = doc.ContainsKey("description") ? doc["description"]?.ToString() : null,
                    Content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? string.Empty : string.Empty,
                    PublishedAt = doc.ContainsKey("publishedAt") && doc["publishedAt"] is DateTimeOffset dto ? dto.DateTime : (DateTime?)null
                }, r.Score ?? 0, emb));
            }
            static double CosSim(float[] a, float[] b)
            {
                double dot=0, na=0, nb=0; int len = Math.Min(a.Length,b.Length);
                for(int i=0;i<len;i++){ dot+=a[i]*b[i]; na+=a[i]*a[i]; nb+=b[i]*b[i]; }
                return (na>0 && nb>0) ? dot/(Math.Sqrt(na)*Math.Sqrt(nb)) : 0;
            }
            var reranked = provisional
                .Select(p => (p.Article, Score: p.Embedding!=null && queryEmbedding.Length>0 ? CosSim(p.Embedding, queryEmbedding) : p.Score))
                .OrderByDescending(x=>x.Score)
                .Take(top)
                .ToList();
            return reranked;
        }
    }
}
