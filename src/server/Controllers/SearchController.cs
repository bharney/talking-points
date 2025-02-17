using Azure.Search.Documents;
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
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        private SearchClient _client;
        private IArticleRepository _articleRepository;
        private IKeywordRepository _keywordRepository;
        public SearchController(ILogger<ArticleDetailsController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository, SearchClient searchClient)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
            _client = searchClient;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string searchPhrase)
        {
            if (string.IsNullOrEmpty(searchPhrase))
            {
                return BadRequest("search phrase cannot be null or empty.");
            }

            // get articles based on keyword search
            async Task<List<ArticleDetails>> Search(string searchPhrase)
            {
                var searchResults = await _client.SearchAsync<Keywords>(searchPhrase);

                var articles = new List<ArticleDetails>();
                await foreach (var result in searchResults.Value.GetResultsAsync())
                {
                    var article = await _articleRepository.Get(result.Document.ArticleId);
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
