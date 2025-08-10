using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using talking_points.Repository;
using talking_points.Models;
using StackExchange.Redis;
using Microsoft.ApplicationInsights;

namespace talking_points.Services
{
	public class VectorIngestionHostedService : BackgroundService
	{
		private readonly IServiceProvider _services;
		private readonly ILogger<VectorIngestionHostedService> _logger;
		private readonly TimeSpan _interval;
		private readonly bool _enabled;
		private readonly bool _enableChunks;
		private readonly int _batchSize;
		private readonly int _embedRateLimitPerInterval;
		private readonly TimeSpan _embedRateInterval;
		private const string LastIndexedKey = "vector:lastIndexedId";
		private DateTime _embedWindowStart = DateTime.UtcNow;
		private int _embedWindowCount = 0;

		public VectorIngestionHostedService(IServiceProvider services, IConfiguration config, ILogger<VectorIngestionHostedService> logger)
		{
			_services = services;
			_logger = logger;
			_interval = TimeSpan.FromMinutes(int.TryParse(config["VectorIngestion:IntervalMinutes"], out var m) ? m : 30);
			_enabled = bool.TryParse(config["VectorIngestion:Enabled"], out var e) ? e : true;
			_enableChunks = bool.TryParse(config["VectorIngestion:EnableChunks"], out var ch) ? ch : true;
			_batchSize = int.TryParse(config["VectorIngestion:BatchSize"], out var b) ? b : 200;
			_embedRateLimitPerInterval = int.TryParse(config["VectorIngestion:EmbedRateLimit"], out var rl) ? rl : 300; // embeddings per window
			var rateSeconds = int.TryParse(config["VectorIngestion:EmbedRateWindowSeconds"], out var rs) ? rs : 60;
			_embedRateInterval = TimeSpan.FromSeconds(rateSeconds);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (!_enabled)
			{
				_logger.LogInformation("Vector ingestion disabled.");
				return;
			}

			while (!stoppingToken.IsCancellationRequested)
			{
				using var scope = _services.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
				var vectorIndex = scope.ServiceProvider.GetRequiredService<IVectorIndexService>();
				var telemetry = scope.ServiceProvider.GetService<TelemetryClient>();
				var redis = scope.ServiceProvider.GetRequiredService<IRedisConnectionManager>().GetDatabase();
				IChunkedVectorIndexService? chunkSvc = null;
				if (_enableChunks)
				{
					chunkSvc = scope.ServiceProvider.GetService<IChunkedVectorIndexService>();
				}
				try
				{
					// Ensure indexes inside try so 403 or other failures don't crash host
					await vectorIndex.EnsureIndexAsync();
					if (_enableChunks && chunkSvc != null)
					{
						await chunkSvc.EnsureChunkIndexAsync();
					}
					var lastIdVal = await redis.StringGetAsync(LastIndexedKey);
					var lastId = lastIdVal.HasValue && int.TryParse(lastIdVal, out var lid) ? lid : 0;
					var sw = System.Diagnostics.Stopwatch.StartNew();
					var newArticles = await db.NewsArticles
						.Where(a => a.Id > lastId)
						.OrderBy(a => a.Id)
						.Take(_batchSize)
						.ToListAsync(stoppingToken);
					if (newArticles.Count == 0)
					{
						_logger.LogInformation("Vector ingestion: no new articles");
					}
					else
					{
						await ApplyEmbeddingRateLimit(newArticles.Count, stoppingToken);
						await vectorIndex.UpsertArticlesAsync(newArticles);
						if (_enableChunks && chunkSvc != null)
						{
							foreach (var art in newArticles)
							{
								await ApplyEmbeddingRateLimit();
								await chunkSvc.UpsertArticleChunksAsync(art);
							}
						}
						var maxId = newArticles.Max(a => a.Id);
						await redis.StringSetAsync(LastIndexedKey, maxId);
						_logger.LogInformation("Indexed {count} articles (lastId -> {id}) chunks:{chunks}", newArticles.Count, maxId, _enableChunks);
						telemetry?.TrackMetric("VectorIngestionDocuments", newArticles.Count);
						if (_enableChunks)
							telemetry?.TrackMetric("VectorIngestionChunkedArticles", newArticles.Count);
					}
					sw.Stop();
					telemetry?.TrackMetric("VectorIngestionLoopMs", sw.ElapsedMilliseconds);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error during vector ingestion loop");
				}
				await Task.Delay(_interval, stoppingToken);
			}
		}

		private Task ApplyEmbeddingRateLimit(int upcoming = 1, CancellationToken ct = default)
		{
			// simple fixed window rate limiter
			var now = DateTime.UtcNow;
			if (now - _embedWindowStart > _embedRateInterval)
			{
				_embedWindowStart = now;
				_embedWindowCount = 0;
			}
			_embedWindowCount += upcoming;
			if (_embedWindowCount <= _embedRateLimitPerInterval) return Task.CompletedTask;
			var delay = _embedRateInterval - (now - _embedWindowStart);
			if (delay < TimeSpan.Zero) return Task.CompletedTask;
			_logger.LogInformation("Embedding rate limit reached; delaying {ms}ms", delay.TotalMilliseconds);
			return Task.Delay(delay, ct);
		}
	}
}
