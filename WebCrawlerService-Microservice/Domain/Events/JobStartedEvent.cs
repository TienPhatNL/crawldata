using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Events
{
    public class JobStartedEvent : BaseEvent
    {
        public Guid JobId { get; }
        public Guid UserId { get; }
        public Guid? AssignedAgentId { get; }
        public string[] Urls { get; }
        public CrawlerType CrawlerType { get; }
        public Priority Priority { get; }
        public DateTime StartedAt { get; }

        public JobStartedEvent(CrawlJob job)
        {
            JobId = job.Id;
            UserId = job.UserId;
            AssignedAgentId = job.AssignedAgentId;
            Urls = job.Urls;
            CrawlerType = job.CrawlerType;
            Priority = job.Priority;
            StartedAt = DateTime.UtcNow;
        }
    }
}