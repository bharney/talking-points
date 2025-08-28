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
        public HealthController(IAzureSearchClients clients, IEmbeddingService embedding, ILogger<HealthController> logger)
        {
            _clients = clients;
            _embedding = embedding;
            _logger = logger;
        }

        [HttpGet("search")] // GET /health/search
        public async Task<IActionResult> SearchHealth()
        {
            var info = new List<object>();
            try
            {
                await foreach (var name in _clients.IndexClient.GetIndexNamesAsync())
                {
                    info.Add(new { index = name });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed listing indexes");
                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(new { indexes = info });
        }

        [HttpGet("embedding-dims")] // GET /health/embedding-dims
        public async Task<IActionResult> EmbeddingDims()
        {
            try
            {
                var probe = await _embedding.EmbedAsync("health probe");
                return Ok(new { dimensions = probe.Length });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Embedding probe failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
