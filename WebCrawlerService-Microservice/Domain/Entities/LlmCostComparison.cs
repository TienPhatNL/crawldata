using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Entities
{
    public class LlmCostComparison : BaseAuditableEntity
    {
        public string Provider { get; set; } = null!;  // "openai", "claude", "gemini"
        public string Model { get; set; } = null!;

        // Current pricing (per 1M tokens)
        public decimal InputCostPer1M { get; set; }
        public decimal OutputCostPer1M { get; set; }

        // Performance benchmarks
        public int AvgResponseTimeMs { get; set; }
        public double AvgQualityScore { get; set; }  // 0-1
        public int TotalUsageCount { get; set; } = 0;

        // Cost tracking
        public decimal TotalCostUsd { get; set; } = 0;
        public long TotalInputTokens { get; set; } = 0;
        public long TotalOutputTokens { get; set; } = 0;

        public DateTime? LastUsedAt { get; set; }
        public DateTime? LastPriceUpdate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
