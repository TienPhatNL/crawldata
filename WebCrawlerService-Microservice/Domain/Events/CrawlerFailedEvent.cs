using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Entities;

namespace WebCrawlerService.Domain.Events
{
    public class CrawlerFailedEvent : BaseEvent
    {
        public Guid JobId { get; }
        public Guid UserId { get; }
        public DateTime FailedAt { get; }
        public string ErrorMessage { get; }
        public string? Url { get; }
        public int RetryCount { get; }
        public bool WillRetry { get; }

        public CrawlerFailedEvent(CrawlJob job, string errorMessage, string? url = null)
        {
            JobId = job.Id;
            UserId = job.UserId;
            FailedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            Url = url;
            RetryCount = job.RetryCount;
            WillRetry = job.RetryCount < job.MaxRetries;
        }
    }
}