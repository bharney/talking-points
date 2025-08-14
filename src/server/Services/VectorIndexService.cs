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
		Task UpsertArticlesAsync(IEnumerable<ArticleDetails> articles);
		Task<IReadOnlyList<(ArticleDetails Article, double Score)>> HybridSearchAsync(string query, int top = 10);
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
						var hasVectorConfig = existing.Value.VectorSearch != null
							&& existing.Value.VectorSearch.Profiles?.Count > 0
							&& embField is not null
							&& !string.IsNullOrWhiteSpace(embField.VectorSearchProfileName);

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
						else if (!hasVectorConfig)
						{
							var msg = $"Index '{_indexName}' missing vector search configuration or field profile; recreating to enable vector queries.";
							if (_recreateOnMismatch)
							{
								_logger.LogWarning(msg);
								await _indexClient.DeleteIndexAsync(_indexName);
								// Continue to recreate below
							}
							else
							{
								_logger.LogError(msg + " Set AzureSearch:RecreateOnDimensionMismatch=true to auto-fix.");
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

		public async Task UpsertArticlesAsync(IEnumerable<ArticleDetails> articles)
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

		public async Task<IReadOnlyList<(ArticleDetails Article, double Score)>> HybridSearchAsync(string query, int top = 10)
		{
			// True hybrid: run lexical search with a vector kNN query so we don't depend solely on keyword matches.
			// Treat the incoming text as a literal phrase so characters like '-' don't act as operators (Simple syntax).
			string escaped = (query ?? string.Empty).Replace("\"", "\\\"");
			string searchText = $"\"{escaped}\"";
			var queryEmbedding = await _embeddingService.EmbedAsync(query ?? string.Empty);
			var options = new SearchOptions { Size = top, IncludeTotalCount = true, QueryType = SearchQueryType.Simple, SearchMode = SearchMode.All };
			options.Select.Add("id");
			options.Select.Add("title");
			options.Select.Add("description");
			options.Select.Add("content");
			options.Select.Add("publishedAt");
			options.SearchFields.Add("title");
			options.SearchFields.Add("description");
			options.SearchFields.Add("content");
			// Add the vector query (kNN) against the embedding field.
			try
			{
				options.VectorSearch = new()
				{
					Queries = { new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = Math.Max(top * 5, 100), Fields = { "embedding" } } }
				};
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to attach vector query; proceeding with lexical only for this request.");
			}
			var resp = await _searchClient.SearchAsync<SearchDocument>(searchText, options);
			var results = resp.Value;
			var list = new List<(ArticleDetails Article, double Score)>();
			await foreach (var r in results.GetResultsAsync())
			{
				var doc = r.Document;
				list.Add((new ArticleDetails
				{
					Id = Guid.TryParse(doc.ContainsKey("id") ? doc["id"]?.ToString() : string.Empty, out var guid) ? guid : Guid.Empty,
					Title = doc.ContainsKey("title") ? doc["title"]?.ToString() ?? string.Empty : string.Empty,
					Description = doc.ContainsKey("description") ? (doc["description"]?.ToString() ?? string.Empty) : string.Empty,
					Content = doc.ContainsKey("content") ? doc["content"]?.ToString() ?? string.Empty : string.Empty,
					PublishedAt = doc.ContainsKey("publishedAt") && doc["publishedAt"] is DateTimeOffset dto ? dto.DateTime : (DateTime?)null
				}, r.Score ?? 0));
			}
			// High-likelihood filtering: keep items that contain the literal phrase in title/description/content or fall within top score band
			string phrase = (query ?? string.Empty).Trim();
			var topScore = list.Count > 0 ? list.Max(x => x.Score) : 0.0;
			double relThreshold = topScore * 0.6; // top 60%
			bool ContainsPhrase(ArticleDetails a)
			{
				var cmp = StringComparison.OrdinalIgnoreCase;
				return (a.Title?.IndexOf(phrase, cmp) >= 0)
					|| (a.Description?.IndexOf(phrase, cmp) >= 0)
					|| (a.Content?.IndexOf(phrase, cmp) >= 0);
			}
			var filtered = list
				.Where(t => ContainsPhrase(t.Article) || t.Score >= relThreshold)
				.OrderByDescending(t => t.Score)
				.Take(top)
				.ToList();
			return filtered;
		}
	}
}
