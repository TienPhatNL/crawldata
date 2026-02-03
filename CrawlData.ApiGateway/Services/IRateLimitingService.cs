namespace CrawlData.ApiGateway.Services
{
    public interface IRateLimitingService
    {
        Task<RateLimitResult> IsRequestAllowedAsync(string clientId, HttpContext context);
    }

    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public int RetryAfterSeconds { get; set; }
        public long ResetTime { get; set; }
    }
}