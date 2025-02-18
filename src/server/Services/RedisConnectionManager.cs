using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Azure.Identity;
using talking_points.Models;
using System;
using System.Net.Security;

namespace talking_points.Services
{
    public interface IRedisConnectionManager
    {
        IDatabase GetDatabase();
        ConnectionMultiplexer GetConnection();
    }

    public class RedisConnectionManager : IRedisConnectionManager, IDisposable
    {
        private readonly Lazy<ConnectionMultiplexer> _connection;
        private readonly ILogger<RedisConnectionManager> _logger;
        private bool _disposed;
        private readonly IConfiguration _configuration;

        public RedisConnectionManager(ILogger<RedisConnectionManager> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connection = CreateConnection();
            _configuration = configuration;

        }

        private Lazy<ConnectionMultiplexer> CreateConnection()
        {
            return new Lazy<ConnectionMultiplexer>(() =>
            {
                // Read updated config from appsettings.json
                var connectionString = _configuration.GetValue<string>("redisCacheConnectionString") ?? throw new InvalidOperationException("Redis connection string missing.");
                var redisSection = _configuration.GetSection("RedisSettings");
                var connectTimeout = int.Parse(redisSection["ConnectTimeout"] ?? "30000");
                var syncTimeout = int.Parse(redisSection["SyncTimeout"] ?? "15000");
                var retryCount = int.Parse(redisSection["RetryCount"] ?? "5");

                var configOptions = ConfigurationOptions.Parse(connectionString);
                configOptions.ConnectTimeout = connectTimeout;
                configOptions.SyncTimeout = syncTimeout;
                configOptions.AbortOnConnectFail = false;
                configOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);
                configOptions.ConnectRetry = retryCount;
                configOptions.DefaultDatabase = 0;
                configOptions.Ssl = true;

                try
                {
                    var connection = ConnectionMultiplexer.Connect(configOptions);
                    connection.ConnectionFailed += (sender, e) =>
                    {
                        _logger.LogError("Redis connection failed: {EndPoint}, {FailureType}, {Exception}",
                            e.EndPoint, e.FailureType, e.Exception);
                    };
                    connection.ConnectionRestored += (sender, e) =>
                    {
                        _logger.LogInformation("Redis connection restored: {EndPoint}", e.EndPoint);
                    };
                    connection.ErrorMessage += (sender, e) =>
                    {
                        _logger.LogWarning("Redis error: {EndPoint}, {Message}", e.EndPoint, e.Message);
                    };

                    return connection;
                }
                catch (RedisConnectionException ex)
                {
                    _logger.LogError("Failed to create Redis connection: {Exception}", ex);
                    throw;
                }
            });
        }

        public IDatabase GetDatabase()
        {
            try
            {
                return _connection.Value.GetDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting Redis database: {Exception}", ex);
                throw;
            }
        }

        public ConnectionMultiplexer GetConnection()
        {
            return _connection.Value;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_connection.IsValueCreated)
                {
                    _connection.Value.Close();
                    _connection.Value.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
