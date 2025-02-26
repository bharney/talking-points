using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using StackExchange.Redis;
using talking_points.Models;
using talking_points.Services;
using Newtonsoft.Json;

namespace talking_points.Repository
{
    public class KeywordRepository : IKeywordRepository
    {
        private readonly ApplicationDbContext _Context;
        private readonly ILogger _Logger;
        private readonly IDatabase _cache;

        public KeywordRepository(
            ApplicationDbContext context, 
            ILoggerFactory loggerFactory,
            IRedisConnectionManager redisManager)
        {
            _Context = context;
            _Logger = loggerFactory.CreateLogger("KeywordRepository");
            _cache = redisManager.GetDatabase();
        }

        public async Task<IEnumerable<Keywords>> GetAll()
        {
            var cacheKey = "Keywords:GetAll";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<IEnumerable<Keywords>>(cachedData);
            }

            var keywords = await _Context.Set<Keywords>().ToListAsync();
            await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(keywords), TimeSpan.FromMinutes(10));
            return keywords;
        }

        public async Task<List<Keywords>?> Get(Guid id)
        {
            var cacheKey = $"Keywords:GetByArticleId:{id}";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<List<Keywords>>(cachedData);
            }

            var keywords = await _Context.Set<Keywords>().ToListAsync();
            var filteredKeywords = keywords.Where(x => x.ArticleId == id).ToList();

            if (filteredKeywords.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(filteredKeywords), TimeSpan.FromMinutes(10));
            }
            return filteredKeywords;
        }

        public async Task<Keywords> Insert(Keywords keyword)
        {
            await _Context.Set<Keywords>().AddAsync(keyword);
            await _Context.SaveChangesAsync();
            await InvalidateCache();
            return keyword;
        }

        public async Task<bool> Update(Keywords keyword)
        {
            _Context.Set<Keywords>().Update(keyword);
            var result = await _Context.SaveChangesAsync() > 0;
            if (result)
            {
                await InvalidateCache();
            }
            return result;
        }

        public async Task<bool> Delete(Guid id)
        {
            var keyword = await _Context.Set<Keywords>().FindAsync(id);
            if (keyword == null)
            {
                return false;
            }

            _Context.Set<Keywords>().Remove(keyword);
            var result = await _Context.SaveChangesAsync() > 0;
            if (result)
            {
                await InvalidateCache();
            }
            return result;
        }

        private async Task InvalidateCache()
        {
            var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().First());
            foreach (var key in server.Keys(pattern: "Keywords:*"))
            {
                await _cache.KeyDeleteAsync(key);
            }
        }

        public async Task<List<Keywords>> GetByKeywordText(string keyword)
        {
            var cacheKey = $"Keywords:ByText:{keyword}";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Keywords>>(cachedData, new JsonSerializerOptions(defaults: JsonSerializerDefaults.Web));
            }

            var keywords = await _Context.Set<Keywords>()
                .ToListAsync(); // Fetch all keywords first

            var filteredKeywords = keywords
                .Where(k => k.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList(); // Apply the filter in memory

            if (filteredKeywords.Any())
            {
                await _cache.StringSetAsync(
                    cacheKey, 
                    JsonSerializer.Serialize(filteredKeywords, new JsonSerializerOptions(defaults: JsonSerializerDefaults.Web)), 
                    TimeSpan.FromMinutes(10));
            }

            return filteredKeywords;
        }
    }
}
