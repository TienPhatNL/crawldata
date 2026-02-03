using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities
{
    public class AnalyticsCache : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public string QueryHash { get; set; } = null!;
        public string OriginalQuery { get; set; } = null!;

        public string ResultJson { get; set; } = null!;
        public string ResultType { get; set; } = "text";  // "aggregate", "chart", "table"

        public Guid[] SourceJobIds { get; set; } = Array.Empty<Guid>();
        public DateTime ComputedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; } = 0;

        public decimal ComputationCost { get; set; }
        public int ComputationTimeMs { get; set; }
    }
}
