using StackExchange.Redis;

namespace CrawlData.ApiGateway.Services
{
    public class RateLimitingService : IRateLimitingService
    {
        private readonly IDatabase _database;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitingService> _logger;

        public RateLimitingService(
            IConnectionMultiplexer redis,
            IConfiguration configuration,
            ILogger<RateLimitingService> logger)
        {
            _database = redis.GetDatabase();
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RateLimitResult> IsRequestAllowedAsync(string clientId, HttpContext context)
        {
            try
            {
                var limits = GetRateLimits(context);
                var window = TimeSpan.FromMinutes(1); // 1-minute sliding window
                var now = DateTimeOffset.UtcNow;
                var windowStart = now.Subtract(window);

                var key = $"ratelimit:{clientId}:{now:yyyyMMddHHmm}";
                
                // Get current count
                var currentCount = await _database.StringGetAsync(key);
                var requestCount = currentCount.HasValue ? (int)currentCount : 0;

                if (requestCount >= limits.RequestsPerMinute)
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        Limit = limits.RequestsPerMinute,
                        Remaining = 0,
                        RetryAfterSeconds = 60 - now.Second,
                        ResetTime = now.AddMinutes(1).ToUnixTimeSeconds()
                    };
                }

                // Increment counter
                await _database.StringIncrementAsync(key);
                await _database.KeyExpireAsync(key, window);

                return new RateLimitResult
                {
                    IsAllowed = true,
                    Limit = limits.RequestsPerMinute,
                    Remaining = limits.RequestsPerMinute - requestCount - 1,
                    RetryAfterSeconds = 0,
                    ResetTime = now.AddMinutes(1).ToUnixTimeSeconds()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limits for client {ClientId}", clientId);
                
                // Allow request on error to avoid blocking legitimate traffic
                return new RateLimitResult
                {
                    IsAllowed = true,
                    Limit = 100,
                    Remaining = 99,
                    RetryAfterSeconds = 0,
                    ResetTime = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds()
                };
            }
        }

        private RateLimits GetRateLimits(HttpContext context)
        {
            // Check if user has paid subscription
            var subscriptionTier = context.Items["SubscriptionTier"]?.ToString();
            
            if (!string.IsNullOrEmpty(subscriptionTier) && subscriptionTier != "Free")
            {
                return new RateLimits
                {
                    RequestsPerMinute = _configuration.GetValue<int>("RateLimiting:PaidUserLimits:RequestsPerMinute", 500),
                    RequestsPerHour = _configuration.GetValue<int>("RateLimiting:PaidUserLimits:RequestsPerHour", 10000)
                };
            }

            return new RateLimits
            {
                RequestsPerMinute = _configuration.GetValue<int>("RateLimiting:DefaultLimits:RequestsPerMinute", 100),
                RequestsPerHour = _configuration.GetValue<int>("RateLimiting:DefaultLimits:RequestsPerHour", 1000)
            };
        }
    }

    public class RateLimits
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
    }
}