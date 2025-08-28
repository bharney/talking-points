using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using talking_points.Services.Caching;

namespace talking_points.Services
{
	public interface IEmbeddingService
	{
		Task<float[]> EmbedAsync(string text);
	}

	public class EmbeddingService : IEmbeddingService
	{
		private readonly OpenAIClient _client;
		private readonly string _deployment;
		private readonly ILogger<EmbeddingService> _logger;
		private readonly IEmbeddingCache? _redisCache;
		private readonly bool _enableCache;
		private readonly TimeSpan _ttl;
		private readonly int _maxRetries;
		private readonly TimeSpan _baseDelay;

		public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger, IEmbeddingCache? redisCache = null)
		{
			var endpoint = config["AzureOpenAIEndpoint"] ?? throw new InvalidOperationException("AzureOpenAIEndpoint missing");
			var key = config["AzureOpenAIKey"] ?? throw new InvalidOperationException("AzureOpenAIKey missing");
			_client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
			_deployment = config["AzureOpenAI:EmbeddingDeployment"] ?? "embeddings";
			_logger = logger;
			_redisCache = redisCache;
			_enableCache = bool.TryParse(config["Cache:EnableEmbeddings"], out var ec) ? ec : true;
			_ttl = TimeSpan.FromMinutes(int.TryParse(config["Cache:EmbeddingsTtlMinutes"], out var t) ? t : 10080); // default 7 days
			_maxRetries = int.TryParse(config["AzureOpenAI:EmbeddingMaxRetries"], out var mr) ? Math.Clamp(mr, 0, 8) : 3;
			_baseDelay = TimeSpan.FromMilliseconds(int.TryParse(config["AzureOpenAI:EmbeddingBaseDelayMs"], out var bd) ? Math.Clamp(bd, 50, 5000) : 250);
		}

		public async Task<float[]> EmbedAsync(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();

			if (_enableCache && _redisCache != null)
			{
				try
				{
					var cached = await _redisCache.GetAsync(text);
					if (cached != null) return cached;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Embedding cache get failed; proceeding without cache");
				}
			}

			int attempt = 0;
			while (true)
			{
				attempt++;
				try
				{
					var resp = await _client.GetEmbeddingsAsync(new EmbeddingsOptions(_deployment, new[] { text }));
					var vector = resp.Value.Data[0].Embedding.ToArray();
					if (_enableCache && _redisCache != null)
					{
						try { await _redisCache.SetAsync(text, vector, _ttl); }
						catch (Exception exSet) { _logger.LogWarning(exSet, "Embedding cache set failed"); }
					}
					return vector;
				}
				catch (RequestFailedException rfe) when (IsRetriableStatus(rfe.Status) && attempt <= _maxRetries)
				{
					var delay = ComputeDelay(attempt, rfe);
					_logger.LogWarning(rfe, "Embedding request failed with status {Status}; retry {Attempt}/{Max} after {Delay} ms", rfe.Status, attempt, _maxRetries, (int)delay.TotalMilliseconds);
					await Task.Delay(delay);
					continue;
				}
				catch (Exception ex) when (attempt <= _maxRetries)
				{
					var delay = ComputeDelay(attempt, ex);
					_logger.LogWarning(ex, "Embedding request unexpected error; retry {Attempt}/{Max} after {Delay} ms", attempt, _maxRetries, (int)delay.TotalMilliseconds);
					await Task.Delay(delay);
					continue;
				}
				catch (Exception final)
				{
					_logger.LogError(final, "Embedding request failed after {Attempts} attempts", attempt);
					return Array.Empty<float>();
				}
			}
		}

		private static bool IsRetriableStatus(int status) => status == 408 || status == 429 || status == 500 || status == 502 || status == 503 || status == 504 || status == 401 || status == 403; // include auth/firewall transient

		private TimeSpan ComputeDelay(int attempt, Exception _)
		{
			var jitter = Random.Shared.NextDouble() * 0.25 + 0.75; // 0.75 - 1.0x
			var exp = Math.Pow(2, attempt - 1);
			var delay = TimeSpan.FromMilliseconds(Math.Min(_baseDelay.TotalMilliseconds * exp, 5000) * jitter);
			return delay;
		}
	}
}
