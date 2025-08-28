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
		private readonly IKeywordsSearchClient _keywordsSearchClient;
		private readonly IArticleRepository _articleRepository;
		private readonly IKeywordRepository _keywordRepository;
		private readonly Services.IVectorIndexService _vectorIndex; // vector fallback
		public SearchController(ILogger<SearchController> logger,
								IConfiguration config,
								IArticleRepository articleRepository,
								IKeywordRepository keywordRepository,
								IKeywordsSearchClient keywordsSearchClient,
								Services.IVectorIndexService vectorIndex)
		{
			_logger = logger;
			_config = config;
			_articleRepository = articleRepository;
			_keywordRepository = keywordRepository;
			_keywordsSearchClient = keywordsSearchClient;
			_vectorIndex = vectorIndex;
		}
		[HttpGet]
		public async Task<IActionResult> Index(string searchPhrase)
		{
			if (string.IsNullOrEmpty(searchPhrase))
			{
				return BadRequest("search phrase cannot be null or empty.");
			}

			// get articles based on keyword search
			var sw = System.Diagnostics.Stopwatch.StartNew();
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
			int keywordCount = articles.Count;
			// Vector fallback if keyword recall low
			if (keywordCount == 0 || keywordCount < 3)
			{
				try
				{
					var vectorResults = await _vectorIndex.HybridSearchAsync(searchPhrase, 10);
					foreach (var (Article, Score) in vectorResults)
					{
						if (Article != null && seen.Add(Article.Id))
						{
							articles.Add(Article);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Vector fallback failed for query '{query}'", searchPhrase);
				}
			}
			sw.Stop();
			_logger.LogInformation("Search query '{q}' keywordResults={k} totalReturned={t} elapsedMs={ms}", searchPhrase, keywordCount, articles.Count, sw.ElapsedMilliseconds);
			return Ok(articles);
		}
	}
}
