using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using talking_points.Models;
using talking_points.Repository;
using talking_points.server.Ingestion;
using talking_points.Services;

namespace talking_points.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class IngestionController : ControllerBase
	{
		private readonly NewsApiIngestionService _ingestionService;
		private readonly INewsArticleRepository _newsArticleRepository;
		private readonly IKeywordService _keywordService;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<IngestionController> _logger;
		private static bool _isIngestionLoopRunning = false;
		private static int _requestsMadeToday = 0;
		private static int _maxRequestsPerDay = 100; // default for NewsAPI Developer (free) plan
		private static TimeSpan _minDelay = TimeSpan.FromMinutes(14.5); // default ~14m30s for 100/day
		private static System.DateTime _lastReset = System.DateTime.UtcNow.Date;
		private static System.DateTime? _nextRunUtc = null;
		private static CancellationTokenSource? _cts;
		private readonly IConfiguration _configuration;

		public IngestionController(
			IConfiguration configuration,
			INewsArticleRepository newsArticleRepository,
			IHttpClientFactory httpClientFactory,
			ILogger<IngestionController> logger,
			IServiceScopeFactory scopeFactory,
			IKeywordService keywordService)
		{
			// Use the named client so default headers (User-Agent, X-Api-Key) are applied.
			_ingestionService = new NewsApiIngestionService(httpClientFactory.CreateClient("NewsApi"), configuration);
			_newsArticleRepository = newsArticleRepository;
			_logger = logger;
			_scopeFactory = scopeFactory;
			_configuration = configuration;
			_keywordService = keywordService;

			// Allow configuring loop behavior via configuration
			if (int.TryParse(_configuration["NewsApi:MaxRequestsPerDay"], out var maxReq) && maxReq > 0)
			{
				_maxRequestsPerDay = maxReq;
			}
			// Prefer seconds for more granular control, fallback to minutes if provided
			if (int.TryParse(_configuration["NewsApi:LoopDelaySeconds"], out var delaySeconds) && delaySeconds > 0)
			{
				_minDelay = TimeSpan.FromSeconds(delaySeconds);
			}
			else if (double.TryParse(_configuration["NewsApi:LoopDelayMinutes"], out var delayMinutes) && delayMinutes > 0)
			{
				_minDelay = TimeSpan.FromMinutes(delayMinutes);
			}
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
			await _keywordService.GenerateKeywordsAsync(filtered);
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
			_cts = new CancellationTokenSource();
			var token = _cts.Token;
			_logger?.LogInformation("Starting ingestion loop. Delay between runs: {delay}, MaxRequestsPerDay: {max}", _minDelay, _maxRequestsPerDay);
			Task.Run(async () =>
			{
				while (_isIngestionLoopRunning && !token.IsCancellationRequested)
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
						_nextRunUtc = System.DateTime.UtcNow.Add(untilMidnight);
						_logger?.LogInformation("Max requests reached ({count}/{max}). Sleeping until UTC midnight at {nextRunUtc}", _requestsMadeToday, _maxRequestsPerDay, _nextRunUtc);
						try
						{
							await Task.Delay(untilMidnight, token);
						}
						catch (TaskCanceledException)
						{
							break;
						}
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
								await _keywordService.GenerateKeywordsAsync(filtered);
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
					// Schedule next run and delay
					_nextRunUtc = System.DateTime.UtcNow.Add(_minDelay);
					_logger?.LogInformation("Next ingestion run scheduled at {nextRunUtc} (UTC)", _nextRunUtc);
					try
					{
						await Task.Delay(_minDelay, token);
					}
					catch (TaskCanceledException)
					{
						break;
					}
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
			_cts?.Cancel();
			_logger?.LogInformation("Ingestion loop stop requested.");
			return Ok(new { Message = "Ingestion loop stopped." });
		}

		[HttpGet("status")]
		public IActionResult GetStatus()
		{
			return Ok(new
			{
				Running = _isIngestionLoopRunning,
				RequestsMadeToday = _requestsMadeToday,
				MaxRequestsPerDay = _maxRequestsPerDay,
				LoopDelaySeconds = (int)_minDelay.TotalSeconds,
				LastResetUtc = _lastReset,
				NextRunUtc = _nextRunUtc
			});
		}
	}
}
