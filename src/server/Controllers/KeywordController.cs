using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using talking_points.Models.ViewModel;
using talking_points.Repository;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeywordController : Controller
    {
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        static HttpClient client = new HttpClient();
        private IArticleRepository _articleRepository;
        private IKeywordRepository _keywordRepository;
        public KeywordController(ILogger<ArticleDetailsController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
        }
        [HttpGet]
        public async Task<IActionResult> Details(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest("Keyword cannot be null or empty.");
            }

            var keywordEntities = await _keywordRepository.GetAll();
            var keywordEntity = keywordEntities.FirstOrDefault(k => k.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

            if (keywordEntity == null)
            {
                return NotFound("Keyword not found.");
            }

            var articles = new List<ArticleDetails>();
            var keywordsViewModel = new KeywordsViewModel();
            foreach (var k in keywordEntities.Where(k => k.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                var article = await _articleRepository.Get(k.ArticleId);
                if (article != null)
                {
                    articles.Add(article);
                    keywordsViewModel.Keywords = k;
                }
            }
            keywordsViewModel.ArticleDetails = articles;
            return Ok(keywordsViewModel);
        }
    }
}
