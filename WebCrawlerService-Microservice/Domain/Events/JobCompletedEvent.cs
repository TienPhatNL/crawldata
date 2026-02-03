using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Entities;

namespace WebCrawlerService.Domain.Events
{
    public class JobCompletedEvent : BaseEvent
    {
        public Guid JobId { get; }
        public Guid UserId { get; }
        public DateTime CompletedAt { get; }
        public int UrlsProcessed { get; }
        public int UrlsSuccessful { get; }
        public int UrlsFailed { get; }
        public long TotalContentSize { get; }
        public TimeSpan ProcessingDuration { get; }
        public string? ConversationName { get; }

        public JobCompletedEvent(CrawlJob job)
        {
            JobId = job.Id;
            UserId = job.UserId;
            CompletedAt = job.CompletedAt ?? DateTime.UtcNow;
            UrlsProcessed = job.UrlsProcessed;
            UrlsSuccessful = job.UrlsSuccessful;
            UrlsFailed = job.UrlsFailed;
            TotalContentSize = job.TotalContentSize;
            ProcessingDuration = job.StartedAt.HasValue ? 
                CompletedAt - job.StartedAt.Value : TimeSpan.Zero;
            ConversationName = job.ConversationName;
        }
    }
}