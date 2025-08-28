using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using talking_points.Models.ViewModel;
using talking_points.Repository;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IConfiguration _config;
        private IKeywordsSearchClient _keywordsSearchClient;
        private IArticleRepository _articleRepository;
        private IKeywordRepository _keywordRepository;
        public SearchController(ILogger<SearchController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository, IKeywordsSearchClient keywordsSearchClient)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
            _keywordsSearchClient = keywordsSearchClient;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string searchPhrase)
        {
            if (string.IsNullOrEmpty(searchPhrase))
            {
                return BadRequest("search phrase cannot be null or empty.");
            }

            // get articles based on keyword search
            var searchKeywordResults = await _keywordsSearchClient.SearchAsync<Keywords>(searchPhrase);
            var articles = new List<ArticleDetails>();
            var seen = new HashSet<Guid>();
            await foreach (SearchResult<Keywords> result in searchKeywordResults.GetResultsAsync())
            {
                var article = await _articleRepository.Get(result.Document.ArticleId);
                if (article != null && seen.Add(article.Id))
                {
                    articles.Add(article);
                }
            }
            var searchResults = articles;
            return Ok(searchResults);
        }
    }
}
