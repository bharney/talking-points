using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using talking_points.Models;
using talking_points.Repository;
using talking_points.server.Ingestion;

namespace talking_points.server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class IngestionController : ControllerBase
	{
		private readonly Ingestion.NewsApiIngestionService _ingestionService;
		private readonly Repository.INewsArticleRepository _newsArticleRepository;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<IngestionController> _logger;
		private static bool _isIngestionLoopRunning = false;
		private static int _requestsMadeToday = 0;
		private static readonly int _maxRequestsPerDay = 100; // NewsAPI Developer (free) plan
		private static readonly TimeSpan _minDelay = TimeSpan.FromMinutes(14.5); // ~14m30s for 100/day
		private static System.DateTime _lastReset = System.DateTime.UtcNow.Date;

		public IngestionController(IConfiguration configuration, INewsArticleRepository newsArticleRepository, IHttpClientFactory httpClientFactory, ILogger<IngestionController> logger, IServiceScopeFactory scopeFactory)
		{
			// Use the named client so default headers (User-Agent, X-Api-Key) are applied.
			_ingestionService = new NewsApiIngestionService(httpClientFactory.CreateClient("NewsApi"), configuration);
			_newsArticleRepository = newsArticleRepository;
			_logger = logger;
			_scopeFactory = scopeFactory;
		}

		[HttpPost("ingest")]
		public async Task<IActionResult> IngestTopHeadlines()
		{
			var fetched = await _ingestionService.FetchTopHeadlinesAsync();
			var latest = await _newsArticleRepository.GetLatestPublishedAtAsync();
			var filtered = await _newsArticleRepository.FilterNewerUniqueAsync(fetched, latest);
			if (filtered.Count == 0)
			{
				return Ok(new { Message = "No new articles", LatestKnown = latest });
			}
			await _newsArticleRepository.AddArticlesAsync(filtered);
			return Ok(new { Inserted = filtered.Count, LatestKnown = latest, MaxFetched = filtered.Max(a => a.PublishedAt) });
		}

		[HttpPost("start-loop")]
		public IActionResult StartIngestionLoop()
		{
			if (_isIngestionLoopRunning)
			{
				return BadRequest(new { Message = "Ingestion loop is already running." });
			}
			_isIngestionLoopRunning = true;
			Task.Run(async () =>
			{
				while (_isIngestionLoopRunning)
				{
					// Reset daily counter at midnight UTC
					if (System.DateTime.UtcNow.Date > _lastReset)
					{
						_requestsMadeToday = 0;
						_lastReset = System.DateTime.UtcNow.Date;
					}
					if (_requestsMadeToday >= _maxRequestsPerDay)
					{
						// Wait until next UTC midnight
						var untilMidnight = _lastReset.AddDays(1) - System.DateTime.UtcNow;
						await Task.Delay(untilMidnight);
						continue;
					}
					try
					{
						// Create a fresh scope each iteration so DbContext lifetime is valid
						using (var scope = _scopeFactory.CreateScope())
						{
							var repo = scope.ServiceProvider.GetRequiredService<INewsArticleRepository>();
							var fetched = await _ingestionService.FetchTopHeadlinesAsync();
							var latest = await repo.GetLatestPublishedAtAsync();
							var filtered = await repo.FilterNewerUniqueAsync(fetched, latest);
							if (filtered.Count > 0)
							{
								await repo.AddArticlesAsync(filtered);
								_logger?.LogInformation("Inserted {count} new articles (loop)", filtered.Count);
							}
							else
							{
								_logger?.LogInformation("No new articles found (loop)");
							}
						}
						_requestsMadeToday++;
					}
					catch (System.Exception ex)
					{
						_logger?.LogError(ex, "Error during NewsAPI ingestion loop iteration");
					}
					await Task.Delay(_minDelay);
				}
			});
			return Ok(new { Message = "Ingestion loop started." });
		}

		[HttpPost("stop-loop")]
		public IActionResult StopIngestionLoop()
		{
			if (!_isIngestionLoopRunning)
			{
				return BadRequest(new { Message = "Ingestion loop is not running." });
			}
			_isIngestionLoopRunning = false;
			return Ok(new { Message = "Ingestion loop stopped." });
		}
	}
}
