using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using talking_points.Models.ViewModel;
using talking_points.Repository;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        static HttpClient client = new HttpClient();
        private IArticleRepository _articleRepository;
        private IKeywordRepository _keywordRepository;
        public HomeController(ILogger<ArticleDetailsController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // get the top stories from all new sources for the day
            var articles = await _articleRepository.GetAll();

            // return json object
            var treeViewList = new List<TreeViewModel>();

            foreach (var item in articles)
            {
                var treeView = new TreeViewModel();

                treeView.ArticleDetails = new ArticleDetails()
                {
                    Id = item.Id,
                    Description = item.Description,
                    Source = item.Source,
                    URL = item.URL,
                    Title = item.Title
                };
                var keywords = await _keywordRepository.Get(item.Id);
                if (keywords == null)
                {
                    treeView.Keywords = new List<Keywords>();
                }
                else
                {
                    foreach (var keyword in keywords)
                    {
                        treeView.Keywords = new List<Keywords>()
                        {
                            new Keywords()
                            {
                                Id = keyword.Id,
                                Keyword = keyword.Keyword,
                                ArticleId = item.Id
                            }
                        };
                    };
                }
                treeViewList.Add(treeView);
            }
            return Ok(treeViewList);
        }
    }
}
