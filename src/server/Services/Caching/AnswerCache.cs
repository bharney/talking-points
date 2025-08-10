using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using talking_points.Models;

namespace talking_points.Services.Caching
{
    public interface IAnswerCache
    {
        Task<string?> GetAsync(string query, IEnumerable<NewsArticle> articles);
        Task SetAsync(string query, IEnumerable<NewsArticle> articles, string answer, TimeSpan ttl);
    }

    public class RedisAnswerCache : IAnswerCache
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultTtl;
        public RedisAnswerCache(IRedisConnectionManager manager, IConfiguration config)
        {
            _db = manager.GetDatabase();
            var minutes = int.TryParse(config["Cache:AnswerTtlMinutes"], out var m) ? m : 30;
            _defaultTtl = TimeSpan.FromMinutes(minutes);
        }

        private static string Key(string query, IEnumerable<NewsArticle> articles)
        {
            using var sha = SHA256.Create();
            var normQ = query.Trim().ToLowerInvariant();
            var ids = articles.Select(a => a.Id).OrderBy(i=>i);
            var basis = normQ + "|" + string.Join(',', ids);
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(basis)));
            return $"ans:{hash}";
        }

        public async Task<string?> GetAsync(string query, IEnumerable<NewsArticle> articles)
        {
            var key = Key(query, articles);
            var v = await _db.StringGetAsync(key);
            return v.HasValue ? v.ToString() : null;
        }

        public async Task SetAsync(string query, IEnumerable<NewsArticle> articles, string answer, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(answer)) return;
            var key = Key(query, articles);
            await _db.StringSetAsync(key, answer, ttl == TimeSpan.Zero ? _defaultTtl : ttl);
        }
    }
}
