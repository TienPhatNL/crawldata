using StackExchange.Redis;
using System.Text.Json;

namespace CrawlData.ApiGateway.Services
{
    public class RequestLoggingService : IRequestLoggingService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RequestLoggingService> _logger;

        public RequestLoggingService(
            IConnectionMultiplexer redis,
            ILogger<RequestLoggingService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task LogRequestAsync(HttpContext context, string requestId, DateTime startTime)
        {
            try
            {
                var requestLog = new RequestLog
                {
                    RequestId = requestId,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserId = context.Items["UserId"]?.ToString(),
                    StartTime = startTime,
                    Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                };

                var logJson = JsonSerializer.Serialize(requestLog);
                var key = $"request_log:{requestId}";
                
                await _database.StringSetAsync(key, logJson, TimeSpan.FromHours(24));
                
                _logger.LogDebug("Request logged: {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request {RequestId}", requestId);
            }
        }

        public async Task LogResponseAsync(HttpContext context, string requestId, DateTime endTime, TimeSpan duration)
        {
            try
            {
                var key = $"request_log:{requestId}";
                var existingLog = await _database.StringGetAsync(key);
                
                if (existingLog.HasValue)
                {
                    var requestLog = JsonSerializer.Deserialize<RequestLog>(existingLog!);
                    if (requestLog != null)
                    {
                        requestLog.EndTime = endTime;
                        requestLog.Duration = duration;
                        requestLog.StatusCode = context.Response.StatusCode;
                        requestLog.ResponseHeaders = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

                        var updatedLogJson = JsonSerializer.Serialize(requestLog);
                        await _database.StringSetAsync(key, updatedLogJson, TimeSpan.FromHours(24));
                    }
                }

                // Also log to analytics if this is an API call
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await LogApiCallAnalyticsAsync(context, requestId, duration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response for request {RequestId}", requestId);
            }
        }

        private async Task LogApiCallAnalyticsAsync(HttpContext context, string requestId, TimeSpan duration)
        {
            try
            {
                var userId = context.Items["UserId"]?.ToString();
                if (string.IsNullOrEmpty(userId)) return;

                var analyticsKey = $"api_analytics:{userId}:{DateTime.UtcNow:yyyyMMdd}";
                var analytics = new
                {
                    RequestId = requestId,
                    Endpoint = context.Request.Path.ToString(),
                    Method = context.Request.Method,
                    StatusCode = context.Response.StatusCode,
                    Duration = duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                await _database.ListRightPushAsync(analyticsKey, JsonSerializer.Serialize(analytics));
                await _database.KeyExpireAsync(analyticsKey, TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API analytics for request {RequestId}", requestId);
            }
        }
    }

    public class RequestLog
    {
        public string RequestId { get; set; } = null!;
        public string Method { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string? QueryString { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    }
}