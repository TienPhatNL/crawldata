using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities
{
    public class AgentPool : BaseAuditableEntity
    {
        public string InstanceId { get; set; } = null!;  // "crawl4ai-1", "crawl4ai-2"
        public CrawlerType AgentType { get; set; }
        public AgentStatus Status { get; set; } = AgentStatus.Available;

        public int MaxConcurrentJobs { get; set; } = 5;
        public int CurrentJobCount { get; set; } = 0;
        public double CurrentLoad => MaxConcurrentJobs > 0 ? (double)CurrentJobCount / MaxConcurrentJobs : 0;

        // Health monitoring
        public DateTime? LastHeartbeat { get; set; }
        public HealthStatus HealthStatus { get; set; } = HealthStatus.Initializing;
        public string? HealthMessage { get; set; }

        // Scaling metadata
        public bool IsAutoScaled { get; set; }
        public DateTime? AutoScaleCreatedAt { get; set; }
        public DateTime? ScheduledForRemovalAt { get; set; }

        // Performance metrics
        public int TotalJobsProcessed { get; set; } = 0;
        public int SuccessfulJobs { get; set; } = 0;
        public int FailedJobs { get; set; } = 0;
        public double AverageJobDurationMs { get; set; } = 0;

        // Container info
        public string? ContainerId { get; set; }
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string ConfigurationJson { get; set; } = "{}";

        // Navigation properties
        public virtual ICollection<CrawlJob> AssignedJobs { get; set; } = new List<CrawlJob>();
    }
}
