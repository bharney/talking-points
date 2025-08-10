using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace talking_points.Services.Caching
{
    public interface IEmbeddingCache
    {
        Task<float[]?> GetAsync(string text);
        Task SetAsync(string text, float[] embedding, TimeSpan ttl);
    }

    public class RedisEmbeddingCache : IEmbeddingCache
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultTtl;
        public RedisEmbeddingCache(IRedisConnectionManager manager, IConfiguration config)
        {
            _db = manager.GetDatabase();
            var minutes = int.TryParse(config["Cache:EmbeddingsTtlMinutes"], out var m) ? m : 10080; // 7 days
            _defaultTtl = TimeSpan.FromMinutes(minutes);
        }

        private static string Key(string text)
        {
            using var sha = SHA256.Create();
            var norm = text.Trim().ToLowerInvariant();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(norm)));
            return $"emb:q:{hash}";
        }

        public async Task<float[]?> GetAsync(string text)
        {
            var key = Key(text);
            var raw = await _db.StringGetAsync(key);
            if (!raw.HasValue) return null;
            try
            {
                var bytes = Convert.FromBase64String(raw!);
                if (bytes.Length % 4 != 0) return null;
                var floats = new float[bytes.Length / 4];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
                return floats;
            }
            catch { return null; }
        }

        public async Task SetAsync(string text, float[] embedding, TimeSpan ttl)
        {
            if (embedding == null || embedding.Length == 0) return;
            var key = Key(text);
            var bytes = new byte[embedding.Length * 4];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            var b64 = Convert.ToBase64String(bytes);
            await _db.StringSetAsync(key, b64, ttl == TimeSpan.Zero ? _defaultTtl : ttl);
        }
    }
}
