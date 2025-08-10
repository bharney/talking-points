using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using talking_points.Models;

namespace talking_points.server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngestionController : ControllerBase
    {
    private readonly Ingestion.NewsApiIngestionService _ingestionService;
    private readonly Repository.INewsArticleRepository _newsArticleRepository;
    private static bool _isIngestionLoopRunning = false;
    private static int _requestsMadeToday = 0;
    private static readonly int _maxRequestsPerDay = 100; // NewsAPI Developer (free) plan
    private static readonly TimeSpan _minDelay = TimeSpan.FromMinutes(14.5); // ~14m30s for 100/day
    private static System.DateTime _lastReset = System.DateTime.UtcNow.Date;

        public IngestionController(IConfiguration configuration, Repository.INewsArticleRepository newsArticleRepository)
        {
            _ingestionService = new Ingestion.NewsApiIngestionService(configuration);
            _newsArticleRepository = newsArticleRepository;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestTopHeadlines()
        {
            var newsArticles = await _ingestionService.FetchTopHeadlinesAsync();
            await _newsArticleRepository.AddArticlesAsync(newsArticles);
            return Ok();
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
                        var newsArticles = await _ingestionService.FetchTopHeadlinesAsync();
                        await _newsArticleRepository.AddArticlesAsync(newsArticles);
                        _requestsMadeToday++;
                    }
                    catch (System.Exception ex)
                    {
                        // TODO: Log exception
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
