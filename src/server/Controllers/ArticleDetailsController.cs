using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using talking_points.Models;
using System.Threading.Tasks;
using System.Web;
using talking_points.Repository;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArticleDetailsController : ControllerBase
    {
        private readonly ILogger<ArticleDetailsController> _logger;
        private readonly IConfiguration _config;
        private readonly IArticleRepository _articleRepository;
        private readonly IKeywordRepository _keywordRepository;
        static HttpClient client = new HttpClient();

        public ArticleDetailsController(ILogger<ArticleDetailsController> logger, IConfiguration config, IArticleRepository articleRepository, IKeywordRepository keywordRepository)
        {
            _logger = logger;
            _config = config;
            _articleRepository = articleRepository;
            _keywordRepository = keywordRepository;
        }

        // traverse the top stories and return the text
        [HttpGet(Name = "GetNYTimesTopStories")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        public async Task<List<string>?> NYTimesTopStories()
        {
            _logger.LogInformation("WebScrapeNYTimesTopStories called");
            var apiKey = _config.GetSection("apiKey").Value;
            // make an api call to NYTimes most popular articles endpoint
            Root articles = new Root();
            var articleBody = new List<ArticleDetails>();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("talking points application");
            HttpResponseMessage response = await client.GetAsync($"https://api.nytimes.com/svc/mostpopular/v2/emailed/7.json?api-key={apiKey}");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Most Popular API call to NYTimes successful");
                var content = await response.Content.ReadAsStringAsync();
                // Assuming the response content is a JSON array of strings
                if (!string.IsNullOrEmpty(content))
                {
                    articles = JsonSerializer.Deserialize<Root>(content) ?? new Root();
                }
                else
                {
                    _logger.LogInformation("Most Popular API: No content found");
                    return null;
                }

                if (articles == null)
                {
                    _logger.LogInformation("Most Popular API: No articles found");
                    return null;
                }

                // for each article iterate and get the article details
                foreach (var result in articles.results)
                {
                    var uri = $"https://api.nytimes.com/svc/search/v2/articlesearch.json?fq={HttpUtility.UrlEncode(@$"web_url:(""{result.url}"")")}&api-key={apiKey}";
                    HttpResponseMessage articleResponse = await client.GetAsync(uri);
                    if (articleResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Article Search API call to NYTimes successful");
                        var articleContent = await articleResponse.Content.ReadAsStringAsync();
                        // Assuming the response content is a JSON array of strings
                        var article = JsonSerializer.Deserialize<ArticleResponse>(articleContent);
                        if (article == null)
                        {
                            _logger.LogInformation("Article Search API: No article found");
                            continue;
                        }

                        _logger.LogInformation($"Article Search API Article URL:{result.url}");
                        var articleDetails = new ArticleDetails()
                        {
                            Id = Guid.NewGuid(),
                            Description = article.response.docs[0].lead_paragraph,
                            Abstract = article.response.docs[0].Abstract,
                            Title = result.title,
                            URL = result.url,
                            Source = "NYTimes"
                        };
                        
                        // check if the article is already in the database
                        var articleExists = await _articleRepository.GetByURL(articleDetails.URL);
                        if (articleExists != null)
                        {
                            _logger.LogInformation($"Article already exists in the database: {articleDetails.URL}");
                            continue;
                        }
                        await _articleRepository.Insert(articleDetails);
                        var keywords = result.adx_keywords.Split(';').ToList();
                        if (keywords == null | keywords?.Count == 0)
                        {
                            continue;
                        }
                        foreach (var keyword in keywords)
                        {
                            if (keyword.IsNullOrEmpty())
                            {
                                continue;
                            }
                            
                            await _keywordRepository.Insert(new Keywords()
                            {
                                Id = Guid.NewGuid(),
                                Keyword = keyword,
                                ArticleId = articleDetails.Id
                            });
                        }
                        articleBody.Add(articleDetails);
                    }
                    // sleep 12 seconds for each api call
                    await Task.Delay(12001);
                }
            }
            _logger.LogInformation("Completed NYTimes Article Details");
            return articleBody.Select(x => x.Description).ToList();
        }
    }
}
