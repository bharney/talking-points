using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
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
		private const string LastIndexedKey = "vector:lastIndexedPublishedAt";
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
				// resolve scoped services inside the iteration so they are recreated each time
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
					var lastPubVal = await redis.StringGetAsync(LastIndexedKey);
					DateTime lastPublished = DateTime.MinValue;
					if (lastPubVal.HasValue)
					{
						// Expect round-trip ISO 8601 format
						if (!DateTime.TryParse(lastPubVal.ToString(), null, DateTimeStyles.RoundtripKind, out lastPublished))
						{
							// Fallback: try invariant parse
							DateTime.TryParse(lastPubVal.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out lastPublished);
						}
					}
					var sw = System.Diagnostics.Stopwatch.StartNew();
					var newArticles = await db.ArticleDetails
						.Where(a => a.PublishedAt != null && a.PublishedAt > lastPublished)
						.OrderBy(a => a.PublishedAt)
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
						var maxPublished = newArticles.Max(a => a.PublishedAt ?? DateTime.MinValue);
						await redis.StringSetAsync(LastIndexedKey, maxPublished.ToString("o"));
						_logger.LogInformation("Indexed {count} articles (lastPublished -> {ts}) chunks:{chunks}", newArticles.Count, maxPublished, _enableChunks);
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
