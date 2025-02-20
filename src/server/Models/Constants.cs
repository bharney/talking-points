namespace talking_points.Models
{
    public class Constants
    {
        // Full connection configuration for Azure Redis Cache
        public static readonly string redisCacheEndpoint = "talkingpoints.redis.cache.windows.net:6380";
        public static readonly bool redisUseSsl = true;
        public static readonly int redisConnectTimeout = 30000; // 30 seconds
        public static readonly int redisSyncTimeout = 10000;    // 10 seconds
        public static readonly int redisRetryCount = 5;
    }
}
