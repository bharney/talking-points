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
        private IArticleDetailsSearchClient _articleDetailsSearchClient;
        public SearchController(ILogger<SearchController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository, IKeywordsSearchClient keywordsSearchClient, IArticleDetailsSearchClient articleDetailsSearchClient)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
            _keywordsSearchClient = keywordsSearchClient;
            _articleDetailsSearchClient = articleDetailsSearchClient;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string searchPhrase)
        {
            if (string.IsNullOrEmpty(searchPhrase))
            {
                return BadRequest("search phrase cannot be null or empty.");
            }

            // get articles based on keyword search
            async Task<List<ArticleDetails>> Search(string query)
            {
                var searchKeywordResults = await _keywordsSearchClient.SearchAsync<Keywords>(query);

                var articles = new List<ArticleDetails>();
                await foreach (SearchResult<Keywords> result in searchKeywordResults.GetResultsAsync())
                {
                    var article = await _articleRepository.Get(result.Document.ArticleId);
                    if (article != null)
                    {
                        articles.Add(article);
                    }
                }
                var searchArticleResults = await _articleDetailsSearchClient.SearchAsync<ArticleDetails>(query);
                await foreach (SearchResult<ArticleDetails> result in searchArticleResults.GetResultsAsync())
                {
                    var article = await _articleRepository.Get(result.Document.Id);
                    if (article != null)
                    {
                        articles.Add(article);
                    }
                }
                return articles;
            }

            var searchResults = await Search(searchPhrase);
            return Ok(searchResults);
        }
    }
}
