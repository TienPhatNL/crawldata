using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities
{
    public class AgentScalingConfig : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public CrawlerType AgentType { get; set; }

        // User-defined limits
        public int MinAgents { get; set; } = 1;
        public int MaxAgents { get; set; } = 10;
        public int TargetAgents { get; set; } = 2;  // Manual override

        // Auto-scaling thresholds
        public bool AutoScalingEnabled { get; set; } = true;
        public double ScaleUpThreshold { get; set; } = 0.8;  // 80% load
        public double ScaleDownThreshold { get; set; } = 0.3;  // 30% load
        public int ScaleUpCooldownMinutes { get; set; } = 5;
        public int ScaleDownCooldownMinutes { get; set; } = 10;

        // Cost controls
        public decimal MaxHourlyCost { get; set; }
        public bool PauseWhenLimitReached { get; set; }

        public DateTime? LastScaleUpAt { get; set; }
        public DateTime? LastScaleDownAt { get; set; }
    }
}
