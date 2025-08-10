using Microsoft.AspNetCore.Mvc;
using talking_points.Services;
using StackExchange.Redis;

namespace talking_points.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VectorAdminController : ControllerBase
    {
        private readonly IRedisConnectionManager _redis;
        private const string LastIndexedKey = "vector:lastIndexedId";
        private readonly IVectorIndexService _vectorIndexService;
        private readonly ILogger<VectorAdminController> _logger;
        public VectorAdminController(IRedisConnectionManager redis, IVectorIndexService vectorIndexService, ILogger<VectorAdminController> logger)
        {
            _redis = redis;
            _vectorIndexService = vectorIndexService;
            _logger = logger;
        }

        [HttpPost("reset-checkpoint")]
        public async Task<IActionResult> ResetCheckpoint()
        {
            await _redis.GetDatabase().KeyDeleteAsync(LastIndexedKey);
            _logger.LogInformation("Vector ingestion checkpoint reset.");
            return Ok(new { reset = true });
        }

        [HttpPost("ensure-index")]
        public async Task<IActionResult> EnsureIndex()
        {
            await _vectorIndexService.EnsureIndexAsync();
            return Ok(new { ensured = true });
        }
    }
}
