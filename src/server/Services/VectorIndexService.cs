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
		private readonly bool _recreateOnMismatch;
		private int? _detectedDims;

		public VectorIndexService(IConfiguration config,
								   IEmbeddingService embeddingService,
								   ILogger<VectorIndexService> logger,
								   IAzureSearchClients clients)
		{
			_indexName = config["AzureSearchArticlesIndexName"] ?? "articles-index";
			_embeddingService = embeddingService;
			_logger = logger;
			_indexClient = clients.IndexClient;
			_searchClient = clients.ArticlesIndexClient;
			_recreateOnMismatch = bool.TryParse(config["AzureSearch:RecreateOnDimensionMismatch"], out var r) ? r : true;
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
				if (exists)
				{
					// Index exists; verify dimension alignment instead of returning immediately.
					try
					{
						var existing = await _indexClient.GetIndexAsync(_indexName);
						var embField = existing.Value.Fields.FirstOrDefault(f => f.Name == "embedding");
						var existingDims = embField?.VectorSearchDimensions;

						// If we haven't detected current embedding dims yet, probe now (only if mismatch or unknown)
						if (!_detectedDims.HasValue)
						{
							try
							{
								var probe = await _embeddingService.EmbedAsync("dimension probe");
								_detectedDims = probe?.Length > 0 ? probe.Length : existingDims ?? 1536;
								_logger.LogDebug("Detected embedding dims (existing index {existingDims}, probe {probeDims})", existingDims, _detectedDims);
							}
							catch (Exception ex)
							{
								_logger.LogWarning(ex, "Failed probing embedding dimensions while verifying existing index; falling back to existing index dims {dims}", existingDims);
								_detectedDims = existingDims ?? 1536;
							}
						}

						if (existingDims.HasValue && _detectedDims.HasValue && existingDims.Value != _detectedDims.Value)
						{
							var msg = $"Index '{_indexName}' has embedding dims {existingDims.Value} but model returns {_detectedDims.Value}";
							if (_recreateOnMismatch)
							{
								_logger.LogWarning(msg + ". Deleting and recreating index.");
								await _indexClient.DeleteIndexAsync(_indexName);
								// Continue to recreate below
							}
							else
							{
								_logger.LogError(msg + ". Set AzureSearch:RecreateOnDimensionMismatch=true to auto-fix or align your embedding deployment.");
								return;
							}
						}
						else
						{
							// Dimensions align; ensure embedding field is retrievable (not hidden).
							if (existingDims.HasValue)
							{
								_detectedDims ??= existingDims.Value;
								var existingEmbField = existing.Value.Fields.FirstOrDefault(f => f.Name == "embedding");
								if (existingEmbField != null && existingEmbField.IsHidden == true)
								{
									_logger.LogWarning("Embedding field in index '{index}' is hidden; updating to make it retrievable.", _indexName);
									existingEmbField.IsHidden = false;
									try
									{
										await _indexClient.CreateOrUpdateIndexAsync(existing.Value);
										_logger.LogInformation("Updated index '{index}' to make embedding field retrievable.", _indexName);
									}
									catch (Exception updEx)
									{
										_logger.LogError(updEx, "Failed to update index '{index}' to unhide embedding field.", _indexName);
									}
								}
								else
								{
									_logger.LogDebug("Index '{index}' already exists with dims {dims}; EnsureIndexAsync noop", _indexName, existingDims.Value);
								}
							}
							return; // nothing else to do
						}
					}
					catch (RequestFailedException rex) when (rex.Status == 404)
					{
						// race: listed name but fetch 404; proceed to create
					}
					catch (Exception ex)
					{
						_logger.LogDebug(ex, "Failed to verify existing index '{index}'; will attempt to recreate.", _indexName);
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
					_logger.LogInformation("Detected embedding dimensions for new index '{index}': {dims}", _indexName, _detectedDims);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to detect embedding dimensions for new index; defaulting to 1536");
					_detectedDims = 1536;
				}
			}
			var vectorDimensions = _detectedDims.Value;

			// At this point either index didn't exist or was deleted for mismatch; proceed to (re)create.
			var fields = new List<SearchField>
			{
				new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
				new SearchableField("title") { IsFilterable = true },
				new SearchableField("description"),
				new SearchableField("content"),
				new SimpleField("publishedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
				new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
				{
					IsSearchable = true,
					VectorSearchDimensions = vectorDimensions,
					VectorSearchProfileName = vectorProfileName
				}
			};
			var definition = new SearchIndex(_indexName, fields)
			{
				VectorSearch = new VectorSearch
				{
					Algorithms = { new HnswAlgorithmConfiguration(vectorAlgoName) },
					Profiles = { new VectorSearchProfile(vectorProfileName, vectorAlgoName) }
				}
			};
			await _indexClient.CreateOrUpdateIndexAsync(definition);
		}

		public async Task UpsertArticlesAsync(IEnumerable<NewsArticle> articles)
		{
			var batch = new List<IndexDocumentsAction<SearchDocument>>();
			foreach (var a in articles)
			{
				var text = string.Join("\n", new[] { a.Title, a.Description ?? string.Empty, a.Content });
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
			SearchResults<SearchDocument> results;
			try
			{
				var resp = await _searchClient.SearchAsync<SearchDocument>(query, options);
				results = resp.Value;
			}
			catch (RequestFailedException ex) when (ex.Message.Contains("not a retrievable field", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning("Embedding field not retrievable in index '{index}'. Reissuing search without embeddings; skipping embedding rerank. Consider allowing retrieval or removing from select.", _indexName);
				// Remove embedding from select and disable rerank by clearing queryEmbedding
				for (int i = options.Select.Count - 1; i >= 0; i--)
				{
					if (string.Equals(options.Select[i], "embedding", StringComparison.OrdinalIgnoreCase))
						options.Select.RemoveAt(i);
				}
				queryEmbedding = Array.Empty<float>();
				var resp2 = await _searchClient.SearchAsync<SearchDocument>(query, options);
				results = resp2.Value;
			}
			var provisional = new List<(NewsArticle Article, double Score, float[]? Embedding)>();
			await foreach (var r in results.GetResultsAsync())
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
				double dot = 0, na = 0, nb = 0; int len = Math.Min(a.Length, b.Length);
				for (int i = 0; i < len; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
				return (na > 0 && nb > 0) ? dot / (Math.Sqrt(na) * Math.Sqrt(nb)) : 0;
			}
			var reranked = provisional
				.Select(p => (p.Article, Score: p.Embedding != null && queryEmbedding.Length > 0 ? CosSim(p.Embedding, queryEmbedding) : p.Score))
				.OrderByDescending(x => x.Score)
				.Take(top)
				.ToList();
			return reranked;
		}
	}
}
