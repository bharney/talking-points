using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using StackExchange.Redis;
using talking_points.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using talking_points.Services;

namespace talking_points.Repository
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly ApplicationDbContext _Context;
        private readonly ILogger _Logger;
        private readonly IDatabase _cache;

        public ArticleRepository(
            ApplicationDbContext context, 
            ILoggerFactory loggerFactory,
            IRedisConnectionManager redisManager)
        {
            _Context = context;
            _Logger = loggerFactory.CreateLogger("ArticleRepository");
            _cache = redisManager.GetDatabase();
        }

        public async Task<IEnumerable<ArticleDetails>> GetAll()
        {
            var cacheKey = "ArticleDetails:GetAll";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<IEnumerable<ArticleDetails>>(cachedData);
            }

            var articles = await _Context.Set<ArticleDetails>().ToListAsync();
            await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(articles), TimeSpan.FromMinutes(10));
            return articles;
        }

        public async Task<ArticleDetails> Get(Guid id)
        {
            var cacheKey = $"ArticleDetails:Get:{id}";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ArticleDetails>(cachedData);
            }

            var article = await _Context.Set<ArticleDetails>().FindAsync(id);
            if (article != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(article), TimeSpan.FromMinutes(10));
            }
            return article;
        }

        public async Task<ArticleDetails> Insert(ArticleDetails article)
        {
            await _Context.Set<ArticleDetails>().AddAsync(article);
            await _Context.SaveChangesAsync();
            await InvalidateCache();
            return article;
        }

        public async Task<bool> Update(ArticleDetails article)
        {
            _Context.Set<ArticleDetails>().Update(article);
            var result = await _Context.SaveChangesAsync() > 0;
            if (result)
            {
                await InvalidateCache();
            }
            return result;
        }

        public async Task<bool> Delete(Guid id)
        {
            var article = await _Context.Set<ArticleDetails>().FindAsync(id);
            if (article == null)
            {
                return false;
            }

            _Context.Set<ArticleDetails>().Remove(article);
            var result = await _Context.SaveChangesAsync() > 0;
            if (result)
            {
                await InvalidateCache();
            }
            return result;
        }

        public async Task<ArticleDetails?> GetByURL(string URL)
        {
            var cacheKey = $"ArticleDetails:GetByURL:{URL}";
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ArticleDetails>(cachedData);
            }

            var article = await _Context.Set<ArticleDetails>().FirstOrDefaultAsync(x => x.URL == URL);
            if (article != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonConvert.SerializeObject(article), TimeSpan.FromMinutes(10));
            }
            return article;
        }

        private async Task InvalidateCache()
        {
            var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().First());
            foreach (var key in server.Keys(pattern: "ArticleDetails:*"))
            {
                await _cache.KeyDeleteAsync(key);
            }
        }
    }
}
