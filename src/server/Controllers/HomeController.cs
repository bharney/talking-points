using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using talking_points.Models.ViewModel;
using talking_points.Repository;
using StackExchange.Redis;
using System.Text.Json;
using talking_points.Services;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        private readonly IArticleRepository _articleRepository;
        private readonly IKeywordRepository _keywordRepository;
        private readonly IDatabase _cache;
        private const int DefaultPageSize = 20;
        private const string CacheKeyPrefix = "HomeController:TreeView";

        public HomeController(
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
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            var cacheKey = $"{CacheKeyPrefix}:p{page}:s{pageSize}";
            var cachedResult = await _cache.StringGetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return Ok(JsonSerializer.Deserialize<List<TreeViewModel>>(cachedResult));
            }

            try
            {
                var startTime = DateTime.UtcNow;
                _logger.LogInformation("Starting Index operation at {time}", startTime);

                // Get all articles with pagination
                var articles = (await _articleRepository.GetAll())
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                // Get all keywords once and calculate counts
                var allKeywords = await _keywordRepository.GetAll();
                var keywordCounts = allKeywords
                    .GroupBy(k => k.Keyword)
                    .ToDictionary(g => g.Key, g => g.Count());

                var treeViewList = new List<TreeViewModel>();
                var processedKeywords = new HashSet<string>();

                foreach (var item in articles)
                {
                    var keywords = await _keywordRepository.Get(item.Id);
                    if (keywords == null) continue;

                    // Process distinct keywords for this article
                    var distinctKeywords = keywords
                        .GroupBy(k => k.Keyword)
                        .Select(g => g.First())
                        .Where(k => !processedKeywords.Contains(k.Keyword))
                        .ToList();

                    // Update processed keywords set
                    foreach (var kw in distinctKeywords)
                    {
                        processedKeywords.Add(kw.Keyword);
                    }

                    var treeView = new TreeViewModel
                    {
                        ArticleDetails = new ArticleDetails
                        {
                            Id = item.Id,
                            Description = item.Description,
                            Source = item.Source,
                            URL = item.URL,
                            Title = item.Title
                        },
                        Keywords = distinctKeywords
                            .Select(k => new KeywordsWithCount
                            {
                                Id = k.Id,
                                Keyword = k.Keyword,
                                ArticleId = item.Id,
                                Count = keywordCounts.GetValueOrDefault(k.Keyword, 0)
                            })
                            .ToList()
                    };

                    treeViewList.Add(treeView);
                    
                    _logger.LogDebug("Processed article {articleId} with {distinctCount} distinct keywords", 
                        item.Id, 
                        distinctKeywords.Count);
                }

                // Cache the result
                await _cache.StringSetAsync(
                    cacheKey,
                    JsonSerializer.Serialize(treeViewList),
                    TimeSpan.FromMinutes(5)
                );

                var endTime = DateTime.UtcNow;
                _logger.LogInformation(
                    "Completed Index operation in {duration}ms. Processed {totalKeywords} unique keywords across {articleCount} articles", 
                    (endTime - startTime).TotalMilliseconds,
                    processedKeywords.Count,
                    treeViewList.Count);

                return Ok(treeViewList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Index request");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
