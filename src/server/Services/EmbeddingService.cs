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

        public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger, IEmbeddingCache? redisCache = null)
        {
            _client = new OpenAIClient(new Uri(config["AzureOpenAIEndpoint"]), new AzureKeyCredential(config["AzureOpenAIKey"]));
            _deployment = config["AzureOpenAI:EmbeddingDeployment"] ?? "embeddings";
            _logger = logger;
            _redisCache = redisCache;
            _enableCache = bool.TryParse(config["Cache:EnableEmbeddings"], out var ec) ? ec : true;
            _ttl = TimeSpan.FromMinutes(int.TryParse(config["Cache:EmbeddingsTtlMinutes"], out var t) ? t : 10080); // default 7 days
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

            var resp = await _client.GetEmbeddingsAsync(new EmbeddingsOptions(_deployment, new[] { text }));
            var vector = resp.Value.Data[0].Embedding.ToArray();

            if (_enableCache && _redisCache != null)
            {
                try
                {
                    await _redisCache.SetAsync(text, vector, _ttl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Embedding cache set failed");
                }
            }
            return vector;
        }
    }
}
