using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using talking_points.Models;

namespace talking_points.Services
{
    public interface IChunkedVectorIndexService
    {
        Task EnsureChunkIndexAsync();
        Task UpsertArticleChunksAsync(NewsArticle article);
        Task UpsertArticleChunksAsync(IEnumerable<NewsArticle> articles);
        Task<IReadOnlyList<(NewsArticle Article, double Score, string Snippet)>> ChunkHybridSearchAsync(string query, int topChunks = 15, int topArticles = 10);
    }

    public class ChunkedVectorIndexService : IChunkedVectorIndexService
    {
        private readonly string _chunkIndexName;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _chunkSearchClient;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<ChunkedVectorIndexService> _logger;
        private readonly int _chunkCharSize;
        private readonly int _chunkCharOverlap;

        public ChunkedVectorIndexService(IConfiguration config,
                                        IEmbeddingService embeddingService,
                                        ILogger<ChunkedVectorIndexService> logger,
                                        IAzureSearchClients clients)
        {
            _chunkIndexName = config["AzureSearch:ChunkIndexName"] ?? config["Search:ArticleChunksIndexName"] ?? "article-chunks-index";
            _embeddingService = embeddingService;
            _logger = logger;
            _indexClient = clients.IndexClient;
            _chunkSearchClient = clients.ChunksIndexClient;
            _chunkCharSize = int.TryParse(config["VectorIngestion:ChunkCharSize"], out var c) ? c : 4000; // ~1000 tokens
            _chunkCharOverlap = int.TryParse(config["VectorIngestion:ChunkCharOverlap"], out var o) ? o : 400;
        }

        public async Task EnsureChunkIndexAsync()
        {
            try
            {
                await foreach (var name in _indexClient.GetIndexNamesAsync())
                {
                    if (name == _chunkIndexName) return;
                }
            }
            catch { }
            var vectorDimensions = 1536;
            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String){ IsKey = true, IsFilterable = true },
                new SimpleField("articleId", SearchFieldDataType.Int32){ IsFilterable = true },
                new SearchableField("title") { IsFilterable = true },
                new SearchableField("chunkContent"),
                new SimpleField("chunkOrder", SearchFieldDataType.Int32){ IsFilterable = true, IsSortable = true },
                new SimpleField("publishedAt", SearchFieldDataType.DateTimeOffset){ IsFilterable = true, IsSortable = true },
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = vectorDimensions
                }
            };
            var definition = new SearchIndex(_chunkIndexName, fields);
            await _indexClient.CreateOrUpdateIndexAsync(definition);
        }

        public async Task UpsertArticleChunksAsync(IEnumerable<NewsArticle> articles)
        {
            foreach (var a in articles)
                await UpsertArticleChunksAsync(a);
        }

        public async Task UpsertArticleChunksAsync(NewsArticle article)
        {
            if (article == null || string.IsNullOrWhiteSpace(article.Content)) return;
            var chunks = Chunk(article.Content, _chunkCharSize, _chunkCharOverlap).ToList();
            if (chunks.Count == 0) return;
            var batch = new List<IndexDocumentsAction<SearchDocument>>();
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkText = chunks[i];
                var emb = await _embeddingService.EmbedAsync(chunkText);
                var doc = new SearchDocument
                {
                    ["id"] = $"{article.Id}-{i}",
                    ["articleId"] = article.Id,
                    ["title"] = article.Title,
                    ["chunkContent"] = chunkText,
                    ["chunkOrder"] = i,
                    ["publishedAt"] = article.PublishedAt,
                    ["embedding"] = emb
                };
                batch.Add(IndexDocumentsAction.MergeOrUpload(doc));
            }
            if (batch.Count > 0)
            {
                var result = await _chunkSearchClient.IndexDocumentsAsync(IndexDocumentsBatch.Create<SearchDocument>(batch.ToArray()));
                if (result.Value.Results.Any(r => !r.Succeeded))
                {
                    _logger.LogWarning("Chunk indexing failures: {ids}", string.Join(',', result.Value.Results.Where(r => !r.Succeeded).Select(r => r.Key)));
                }
            }
        }

        public async Task<IReadOnlyList<(NewsArticle Article, double Score, string Snippet)>> ChunkHybridSearchAsync(string query, int topChunks = 15, int topArticles = 10)
        {
            var queryEmbedding = await _embeddingService.EmbedAsync(query);
            var options = new SearchOptions { Size = topChunks };
            options.Select.Add("id");
            options.Select.Add("articleId");
            options.Select.Add("title");
            options.Select.Add("chunkContent");
            options.Select.Add("publishedAt");
            options.Select.Add("embedding");
            var results = await _chunkSearchClient.SearchAsync<SearchDocument>(query, options);
            var chunkHits = new List<(int ArticleId, string Title, string Chunk, DateTime? PublishedAt, double LexScore, float[]? Emb)>();
            await foreach (var r in results.Value.GetResultsAsync())
            {
                var doc = r.Document;
                float[]? emb = null;
                if (doc.TryGetValue("embedding", out var embObj) && embObj is IEnumerable<float> floats)
                    emb = floats.ToArray();
                int articleId = doc.ContainsKey("articleId") && int.TryParse(doc["articleId"].ToString(), out var aid) ? aid : 0;
                DateTime? published = doc.ContainsKey("publishedAt") && doc["publishedAt"] is DateTimeOffset dto ? dto.DateTime : (DateTime?)null;
                chunkHits.Add((articleId,
                                doc.ContainsKey("title") ? doc["title"]?.ToString() ?? string.Empty : string.Empty,
                                doc.ContainsKey("chunkContent") ? doc["chunkContent"]?.ToString() ?? string.Empty : string.Empty,
                                published,
                                r.Score ?? 0,
                                emb));
            }
            static double CosSim(float[] a, float[] b)
            {
                double dot=0, na=0, nb=0; int len = Math.Min(a.Length,b.Length);
                for(int i=0;i<len;i++){ dot+=a[i]*b[i]; na+=a[i]*a[i]; nb+=b[i]*b[i]; }
                return (na>0 && nb>0) ? dot/(Math.Sqrt(na)*Math.Sqrt(nb)) : 0;
            }
            var reranked = chunkHits.Select(h => new {
                h.ArticleId,
                h.Title,
                h.Chunk,
                h.PublishedAt,
                Score = (h.Emb!=null && queryEmbedding.Length>0) ? CosSim(h.Emb, queryEmbedding) : h.LexScore
            })
            .OrderByDescending(x=>x.Score)
            .Take(topChunks)
            .ToList();

            var grouped = reranked
                .GroupBy(x=>x.ArticleId)
                .Select(g => (
                    Article: new NewsArticle { Id = g.Key, Title = g.First().Title, Content = g.First().Chunk, PublishedAt = g.First().PublishedAt },
                    Score: g.Max(x=>x.Score),
                    Snippet: g.First().Chunk.Length > 400 ? g.First().Chunk.Substring(0,400) + "..." : g.First().Chunk))
                .OrderByDescending(t=>t.Score)
                .Take(topArticles)
                .ToList();
            return grouped;
        }

        private static IEnumerable<string> Chunk(string text, int size, int overlap)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            int pos = 0;
            int len = text.Length;
            while (pos < len)
            {
                int take = Math.Min(size, len - pos);
                var segment = text.Substring(pos, take);
                yield return segment;
                if (pos + take >= len) break;
                pos += size - overlap;
                if (pos < 0 || pos >= len) break;
            }
        }
    }
}
