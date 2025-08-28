using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using talking_points.Services;

namespace talking_points.Controllers
{
	[ApiController]
	[Route("health")] // /health endpoints
	public class HealthController : ControllerBase
	{
		private readonly IAzureSearchClients _clients;
		private readonly IEmbeddingService _embedding;
		private readonly ILogger<HealthController> _logger;
		private readonly IConfiguration _config;
		private readonly IWebHostEnvironment _env;
		public HealthController(IAzureSearchClients clients,
								IEmbeddingService embedding,
								ILogger<HealthController> logger,
								IConfiguration config,
								IWebHostEnvironment env)
		{
			_clients = clients;
			_embedding = embedding;
			_logger = logger;
			_config = config;
			_env = env;
		}

		[HttpGet("search")] // GET /health/search
		public async Task<IActionResult> SearchHealth()
		{
			var indexes = new List<object>();
			try
			{
				await foreach (var name in _clients.IndexClient.GetIndexNamesAsync())
				{
					long? docCount = null;
					try
					{
						var stats = await _clients.IndexClient.GetIndexStatisticsAsync(name);
						docCount = stats.Value.DocumentCount;
					}
					catch (Exception statEx)
					{
						_logger.LogWarning(statEx, "Failed to get stats for index {index}", name);
					}
					indexes.Add(new { index = name, documents = docCount });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed listing indexes");
				return StatusCode(500, new { error = ex.Message });
			}
			return Ok(new { indexes });
		}

		[HttpGet("embedding-dims")] // GET /health/embedding-dims
		public async Task<IActionResult> EmbeddingDims()
		{
			try
			{
				var probe = await _embedding.EmbedAsync("health probe");
				var dims = probe.Length;
				var diag = (_embedding as EmbeddingService)?.GetLastErrorInfo();
				return Ok(new
				{
					dimensions = dims,
					success = dims > 0,
					note = dims == 0 ? "Embedding service returned empty vector (likely config / auth / deployment issue)." : null,
					lastStatus = diag?.status,
					lastError = diag?.error,
					lastErrorAtUtc = diag?.atUtc,
					deployment = diag?.deployment
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Embedding probe failed");
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("embedding-test")] // GET /health/embedding-test?text=...
		public async Task<IActionResult> EmbeddingTest([FromQuery] string text = "Sample test text")
		{
			var started = DateTime.UtcNow;
			float[] vec = Array.Empty<float>();
			try
			{
				vec = await _embedding.EmbedAsync(text);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Embedding test threw exception");
				return StatusCode(500, new { error = ex.Message, elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds });
			}
			var diag = (_embedding as EmbeddingService)?.GetLastErrorInfo();
			return Ok(new
			{
				inputLength = text.Length,
				vectorLength = vec.Length,
				first8 = vec.Take(8).ToArray(),
				elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds,
				warning = vec.Length == 0 ? "Empty vector returned; check deployment name, key, network, or model type." : null,
				lastStatus = diag?.status,
				lastError = diag?.error,
				lastErrorAtUtc = diag?.atUtc,
				deployment = diag?.deployment
			});
		}
	}
}
