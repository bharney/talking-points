using Microsoft.AspNetCore.Http;
// No changes needed in this file as the method signature and usage remain the same.
using Microsoft.AspNetCore.Mvc;
using talking_points.Models.ViewModel;
using talking_points.Repository;
using talking_points.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeywordController : Controller
    {
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        private readonly IArticleRepository _articleRepository;
        private readonly IKeywordRepository _keywordRepository;
        private readonly IDatabase _cache;
        private const string CacheKeyPrefix = "KeywordController:Details";
        private const int DefaultPageSize = 20;

        public KeywordController(
            ILogger<ArticleDetailsController> logger,
            IConfiguration config,
            IArticleRepository articleRepository,
            IKeywordRepository keywordRepository,
            IRedisConnectionManager redisManager)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
            _cache = redisManager.GetDatabase();
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            [FromQuery] string keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = DefaultPageSize)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest("Keyword cannot be null or empty.");
            }

            var cacheKey = $"{CacheKeyPrefix}:{keyword}:p{page}:s{pageSize}";

            try
            {
                var startTime = DateTime.UtcNow;
                _logger.LogInformation("Starting keyword search for '{keyword}' at {time}", keyword, startTime);

                // Check cache first
                var cachedResult = await _cache.StringGetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    _logger.LogInformation("Cache hit for keyword '{keyword}'", keyword);
                    return Ok(JsonSerializer.Deserialize<KeywordDetailsResponse>(cachedResult, new JsonSerializerOptions(defaults: JsonSerializerDefaults.Web)));
                }

                // Use the new optimized method
                var keywordEntities = await _keywordRepository.GetByKeywordText(keyword);

                if (!keywordEntities.Any())
                {
                    _logger.LogInformation("No results found for keyword '{keyword}'", keyword);
                    return NotFound("Keyword not found.");
                }

                // Rest of the code remains the same ...
                var articleIds = keywordEntities
                    .Select(k => k.ArticleId)
                    .Distinct()
                    .ToList();

                var totalArticles = articleIds.Count;
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                var paginatedArticleIds = articleIds
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Fetch articles in parallel
                var articleTasks = new List<Task<ArticleDetails>>();

                var articles = (await _articleRepository.GetRange(paginatedArticleIds)).Where(article => article != null).ToList();

                var response = new KeywordDetailsResponse
                {
                    Keyword = keywordEntities.First(),
                    Articles = articles,
                    TotalArticles = totalArticles,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                // Cache the result
                await _cache.StringSetAsync(
                    cacheKey,
                    JsonSerializer.Serialize(response),
                    TimeSpan.FromMinutes(5)
                );

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation(
                    "Completed keyword search for '{keyword}' in {duration}ms. Found {articleCount} articles",
                    keyword, duration, articles.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing keyword search for '{keyword}'", keyword);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
