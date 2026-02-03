using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities
{
    public class CrawlerAgent : BaseAuditableEntity
    {
        public string Name { get; set; } = null!;
        public CrawlerType Type { get; set; }
        public AgentStatus Status { get; set; } = AgentStatus.Available;
        
        public string ConfigurationJson { get; set; } = "{}";
        public string? UserAgent { get; set; }
        public int MaxConcurrentJobs { get; set; } = 5;
        public int CurrentJobCount { get; set; } = 0;
        
        // Performance metrics
        public int TotalJobsProcessed { get; set; } = 0;
        public int SuccessfulJobs { get; set; } = 0;
        public int FailedJobs { get; set; } = 0;
        public double AverageProcessingTime { get; set; } = 0;
        public DateTime? LastHealthCheck { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public DateTime? LastAssignedAt { get; set; }
        
        // Resource limits
        public int MaxMemoryMB { get; set; } = 512;
        public int MaxCpuPercent { get; set; } = 80;
        
        // Navigation properties
        public virtual ICollection<CrawlJob> AssignedJobs { get; set; } = new List<CrawlJob>();
        public virtual ICollection<CrawlJob> ActiveJobs => AssignedJobs.Where(j => j.Status == Enums.JobStatus.Running || j.Status == Enums.JobStatus.Queued).ToList();
    }
}