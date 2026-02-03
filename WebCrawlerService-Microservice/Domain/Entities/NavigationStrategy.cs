using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities
{
    public class NavigationStrategy : BaseAuditableEntity
    {
        public string Name { get; set; } = null!;
        public string Domain { get; set; } = null!;
        public string UrlPattern { get; set; } = null!;

        public NavigationStrategyType Type { get; set; }
        public string NavigationStepsJson { get; set; } = "[]";

        // Performance tracking
        public int TimesUsed { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;
        public int FailureCount { get; set; } = 0;
        public double AverageExecutionTimeMs { get; set; } = 0;
        public double SuccessRate => TimesUsed > 0 ? (double)SuccessCount / TimesUsed : 0;

        // Learning metadata
        public Guid? CreatedByJobId { get; set; }
        public Guid? LastUsedByJobId { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // Template management
        public bool IsTemplate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Tags { get; set; }  // "e-commerce,product-list,pagination"

        // Navigation properties
        public virtual CrawlJob? CreatedByJob { get; set; }
        public virtual ICollection<CrawlJob> UsedByJobs { get; set; } = new List<CrawlJob>();
    }
}
