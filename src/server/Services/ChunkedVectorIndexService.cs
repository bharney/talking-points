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
		Task UpsertArticleChunksAsync(ArticleDetails article);
		Task UpsertArticleChunksAsync(IEnumerable<ArticleDetails> articles);
		Task<IReadOnlyList<(ArticleDetails Article, double Score, string Snippet)>> ChunkHybridSearchAsync(string query, int topChunks = 15, int topArticles = 10);
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
		private readonly bool _recreateOnMismatch;
		private int? _detectedDims;

		public ChunkedVectorIndexService(IConfiguration config,
										IEmbeddingService embeddingService,
										ILogger<ChunkedVectorIndexService> logger,
										IAzureSearchClients clients)
		{
			_chunkIndexName = config["AzureSearchChunkIndexName"] ?? "article-chunks-index";
			_embeddingService = embeddingService;
			_logger = logger;
			_indexClient = clients.IndexClient;
			_chunkSearchClient = clients.ChunksIndexClient;
			_chunkCharSize = int.TryParse(config["VectorIngestion:ChunkCharSize"], out var c) ? c : 4000; // ~1000 tokens
			_chunkCharOverlap = int.TryParse(config["VectorIngestion:ChunkCharOverlap"], out var o) ? o : 400;
			_recreateOnMismatch = bool.TryParse(config["AzureSearch:RecreateOnDimensionMismatch"], out var r) ? r : true; // default true for safety
		}

		public async Task EnsureChunkIndexAsync()
		{
			try
			{
				var exists = false;
				await foreach (var name in _indexClient.GetIndexNamesAsync())
				{
					if (name == _chunkIndexName)
					{
						exists = true;
						break;
					}
				}
				if (exists)
				{
					try
					{
						var existing = await _indexClient.GetIndexAsync(_chunkIndexName);
						var embField = existing.Value.Fields.FirstOrDefault(f => f.Name == "embedding");
						var existingDims = embField?.VectorSearchDimensions;
						var hasVectorConfig = existing.Value.VectorSearch != null
							&& existing.Value.VectorSearch.Profiles?.Count > 0
							&& embField is not null
							&& !string.IsNullOrWhiteSpace(embField.VectorSearchProfileName);

						// If articleId field exists with wrong type (Int32), recreate index to switch to String (GUID)
						var articleIdField = existing.Value.Fields.FirstOrDefault(f => f.Name == "articleId");
						var needsArticleIdFix = articleIdField != null && articleIdField.Type == SearchFieldDataType.Int32;
						if (!_detectedDims.HasValue)
						{
							try
							{
								var probe = await _embeddingService.EmbedAsync("dimension probe");
								_detectedDims = probe?.Length > 0 ? probe.Length : existingDims ?? 1536;
								_logger.LogDebug("Detected chunk embedding dims (existing {existingDims}, probe {probeDims})", existingDims, _detectedDims);
							}
							catch (Exception ex)
							{
								_logger.LogWarning(ex, "Failed probing embedding dimensions for chunk index; falling back to existing dims {dims}", existingDims);
								_detectedDims = existingDims ?? 1536;
							}
						}
						if (existingDims.HasValue && _detectedDims.HasValue && existingDims.Value != _detectedDims.Value)
						{
							var msg = $"Chunk index '{_chunkIndexName}' has embedding dims {existingDims.Value} but model returns {_detectedDims.Value}";
							if (_recreateOnMismatch)
							{
								_logger.LogWarning(msg + ". Deleting and recreating chunk index.");
								await _indexClient.DeleteIndexAsync(_chunkIndexName);
								// proceed to recreate
							}
							else
							{
								_logger.LogError(msg + ". Set AzureSearch:RecreateOnDimensionMismatch=true to auto-fix or align your embedding deployment.");
								return;
							}
						}
						else if (!hasVectorConfig || needsArticleIdFix)
						{
							var msg = needsArticleIdFix
								? $"Chunk index '{_chunkIndexName}' has articleId as Int32; recreating to change it to String (GUID)."
								: $"Chunk index '{_chunkIndexName}' missing vector search configuration or field profile; recreating to enable vector queries.";
							if (_recreateOnMismatch)
							{
								_logger.LogWarning(msg);
								await _indexClient.DeleteIndexAsync(_chunkIndexName);
								// proceed to recreate
							}
							else
							{
								_logger.LogError(msg + " Set AzureSearch:RecreateOnDimensionMismatch=true to auto-fix.");
								return;
							}
						}
						else
						{
							if (existingDims.HasValue)
							{
								_detectedDims ??= existingDims.Value;
								var existingEmbField = existing.Value.Fields.FirstOrDefault(f => f.Name == "embedding");
								if (existingEmbField != null && existingEmbField.IsHidden == true)
								{
									_logger.LogWarning("Embedding field in chunk index '{index}' is hidden; updating to make it retrievable.", _chunkIndexName);
									existingEmbField.IsHidden = false;
									try
									{
										await _indexClient.CreateOrUpdateIndexAsync(existing.Value);
										_logger.LogInformation("Updated chunk index '{index}' to make embedding field retrievable.", _chunkIndexName);
									}
									catch (Exception updEx)
									{
										_logger.LogError(updEx, "Failed to update chunk index '{index}' to unhide embedding field.", _chunkIndexName);
									}
								}
								else
								{
									_logger.LogDebug("Chunk index '{index}' already exists with dims {dims}; EnsureChunkIndexAsync noop", _chunkIndexName, existingDims.Value);
								}
							}
							return; // aligned, nothing to do
						}
					}
					catch (RequestFailedException rex) when (rex.Status == 404)
					{
						// race; will create below
					}
					catch (Exception ex)
					{
						_logger.LogDebug(ex, "Failed to verify existing chunk index '{index}'; will attempt to recreate.", _chunkIndexName);
					}
				}
			}
			catch { /* continue to create */ }

			const string vectorProfileName = "default-vector-profile";
			const string vectorAlgoName = "vector-hnsw";
			if (!_detectedDims.HasValue)
			{
				try
				{
					var probe = await _embeddingService.EmbedAsync("dimension probe");
					_detectedDims = probe?.Length > 0 ? probe.Length : 1536;
					_logger.LogInformation("Detected embedding dimensions for new chunk index '{index}': {dims}", _chunkIndexName, _detectedDims);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to detect embedding dimensions for new chunk index; defaulting to 1536");
					_detectedDims = 1536;
				}
			}
			var vectorDimensions = _detectedDims.Value;

			var fields = new List<SearchField>
			{
				new SimpleField("id", SearchFieldDataType.String){ IsKey = true, IsFilterable = true },
				new SimpleField("articleId", SearchFieldDataType.String){ IsFilterable = true },
				new SearchableField("title") { IsFilterable = true },
				new SearchableField("chunkContent"),
				new SimpleField("chunkOrder", SearchFieldDataType.Int32){ IsFilterable = true, IsSortable = true },
				new SimpleField("publishedAt", SearchFieldDataType.DateTimeOffset){ IsFilterable = true, IsSortable = true },
				new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
				{
					IsSearchable = true,
					VectorSearchDimensions = vectorDimensions,
					VectorSearchProfileName = vectorProfileName
				}
			};
			var definition = new SearchIndex(_chunkIndexName, fields)
			{
				VectorSearch = new VectorSearch
				{
					Algorithms = { new HnswAlgorithmConfiguration(vectorAlgoName) },
					Profiles = { new VectorSearchProfile(vectorProfileName, vectorAlgoName) }
				}
			};
			await _indexClient.CreateOrUpdateIndexAsync(definition);
		}

		public async Task UpsertArticleChunksAsync(IEnumerable<ArticleDetails> articles)
		{
			foreach (var a in articles)
				await UpsertArticleChunksAsync(a);
		}

		public async Task UpsertArticleChunksAsync(ArticleDetails article)
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
					["articleId"] = article.Id.ToString(),
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

		public async Task<IReadOnlyList<(ArticleDetails Article, double Score, string Snippet)>> ChunkHybridSearchAsync(string query, int topChunks = 15, int topArticles = 10)
		{
			// Quote the query to treat it as a literal phrase in Simple syntax so characters like '-' don't act as operators.
			string escaped = (query ?? string.Empty).Replace("\"", "\\\"");
			string searchText = $"\"{escaped}\"";

			var queryEmbedding = await _embeddingService.EmbedAsync(query ?? string.Empty);
			var options = new SearchOptions { Size = topChunks, QueryType = SearchQueryType.Simple, SearchMode = SearchMode.All };
			options.Select.Add("id");
			options.Select.Add("articleId");
			options.Select.Add("title");
			options.Select.Add("chunkContent");
			options.Select.Add("publishedAt");
			options.SearchFields.Add("title");
			options.SearchFields.Add("chunkContent");
			// Add highlighting for better snippets
			options.HighlightFields.Add("chunkContent");
			options.HighlightPreTag = "<em>";
			options.HighlightPostTag = "</em>";
			// Attach a vector query so chunks are returned even when lexical match is weak.
			try
			{
				options.VectorSearch = new()
				{
					Queries = { new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = Math.Max(topChunks * 5, 100), Fields = { "embedding" } } }
				};
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to attach vector query to chunk search; proceeding lexical-only.");
			}
			var resp = await _chunkSearchClient.SearchAsync<SearchDocument>(searchText, options);
			var searchResults = resp.Value;
			var chunkHits = new List<(string ArticleId, string Title, string Chunk, DateTime? PublishedAt, double Score, string? Highlight)>();
			await foreach (var r in searchResults.GetResultsAsync())
			{
				var doc = r.Document;
				string articleId = doc.ContainsKey("articleId") ? doc["articleId"]?.ToString() ?? string.Empty : string.Empty;
				DateTime? published = doc.ContainsKey("publishedAt") && doc["publishedAt"] is DateTimeOffset dto ? dto.DateTime : (DateTime?)null;
				var title = doc.ContainsKey("title") ? doc["title"]?.ToString() ?? string.Empty : string.Empty;
				var chunk = doc.ContainsKey("chunkContent") ? doc["chunkContent"]?.ToString() ?? string.Empty : string.Empty;
				var highlight = r.Highlights != null && r.Highlights.TryGetValue("chunkContent", out var hl) && hl?.Count > 0 ? hl[0] : null;
				chunkHits.Add((articleId, title, chunk, published, r.Score ?? 0, highlight));
			}

			var grouped = chunkHits
				.GroupBy(x => x.ArticleId)
				.Select(g =>
				{
					var best = g.OrderByDescending(x => x.Score).First();
					var snippet = best.Highlight ?? (best.Chunk.Length > 400 ? best.Chunk.Substring(0, 400) + "..." : best.Chunk);
					return (
						Article: new ArticleDetails
						{
							Id = Guid.TryParse(g.Key, out var guid) ? guid : Guid.Empty,
							Title = best.Title,
							Content = best.Chunk,
							PublishedAt = best.PublishedAt
						},
						Score: best.Score,
						Snippet: snippet
					);
				})
				.OrderByDescending(t => t.Score)
				.ToList();

			// Filter to high-likelihood results: keep those containing the literal phrase, or within a relative score band.
			string phrase = (query ?? string.Empty).Trim();
			var topScore = grouped.Count > 0 ? grouped.Max(x => x.Score) : 0;
			double relThreshold = topScore * 0.6; // keep top 60% band
			bool ContainsPhrase((ArticleDetails Article, double Score, string Snippet) item)
			{
				if (string.IsNullOrEmpty(phrase)) return true;
				var cmp = StringComparison.OrdinalIgnoreCase;
				return (item.Article.Title?.IndexOf(phrase, cmp) >= 0)
					|| (item.Article.Content?.IndexOf(phrase, cmp) >= 0)
					|| (item.Snippet?.IndexOf(phrase, cmp) >= 0);
			}
			var filtered = grouped
				.Where(it => ContainsPhrase(it) || it.Score >= relThreshold)
				.OrderByDescending(it => it.Score)
				.Take(topArticles)
				.ToList();
			return filtered;
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
