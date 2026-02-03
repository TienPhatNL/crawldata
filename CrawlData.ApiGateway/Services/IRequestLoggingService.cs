namespace CrawlData.ApiGateway.Services
{
    public interface IRequestLoggingService
    {
        Task LogRequestAsync(HttpContext context, string requestId, DateTime startTime);
        Task LogResponseAsync(HttpContext context, string requestId, DateTime endTime, TimeSpan duration);
    }
}