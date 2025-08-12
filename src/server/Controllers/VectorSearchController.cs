using Microsoft.AspNetCore.Mvc;
using talking_points.Services;
using talking_points.Models;

namespace talking_points.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class VectorSearchController : ControllerBase
	{
		private readonly IVectorIndexService _vectorIndexService;
		private readonly IRagAnswerService _ragAnswerService;
		private readonly IChunkedVectorIndexService? _chunked;
		private readonly ILogger<VectorSearchController> _logger;
		public VectorSearchController(IVectorIndexService vectorIndexService, IRagAnswerService ragAnswerService, ILogger<VectorSearchController> logger, IChunkedVectorIndexService? chunkedVectorIndexService = null)
		{
			_vectorIndexService = vectorIndexService;
			_ragAnswerService = ragAnswerService;
			_logger = logger;
			_chunked = chunkedVectorIndexService;
		}

		[HttpGet("hybrid")] // /VectorSearch/hybrid?query=...
		public async Task<IActionResult> Hybrid(string query, int top = 10, bool includeAnswer = false)
		{
			if (string.IsNullOrWhiteSpace(query)) return BadRequest("query required");
			var results = await _vectorIndexService.HybridSearchAsync(query, top);
			string? answer = null;
			if (includeAnswer)
			{
				var answerArticles = results.Take(Math.Min(5, results.Count)).Select(r => r.Article).ToList();
				answer = await _ragAnswerService.CreateAnswerAsync(query, answerArticles);
			}
			return Ok(new
			{
				query,
				results = results.Select(r => new { r.Article.Id, r.Article.Title, r.Article.Description, r.Article.Url, r.Article.SourceName, r.Article.PublishedAt, score = r.Score }),
				answer
			});
		}
		[HttpGet("chunk-hybrid")] // /VectorSearch/chunk-hybrid?query=...
		public async Task<IActionResult> ChunkHybrid(string query, int topChunks = 15, int topArticles = 10, bool includeAnswer = false)
		{
			if (_chunked == null) return StatusCode(501, "Chunked vector index service not configured.");
			if (string.IsNullOrWhiteSpace(query)) return BadRequest("query required");
			var results = await _chunked.ChunkHybridSearchAsync(query, topChunks, topArticles);
			string? answer = null;
			if (includeAnswer)
			{
				var articles = results.Select(r => r.Article).ToList();
				answer = await _ragAnswerService.CreateAnswerAsync(query, articles);
			}
			return Ok(new
			{
				query,
				results = results.Select(r => new { r.Article.Id, r.Article.Title, r.Article.Description, r.Article.Url, r.Article.SourceName, r.Snippet, r.Article.PublishedAt, score = r.Score }),
				answer
			});
		}
	}
}
