using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Domain.Entities
{
    public class PromptHistory : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid? CrawlJobId { get; set; }  // Null for cross-job analytics
        public Guid? ConversationId { get; set; }  // Group related prompts

        public PromptType Type { get; set; }
        public string PromptText { get; set; } = null!;
        public string? ResponseText { get; set; }
        public string? ResponseDataJson { get; set; }

        public DateTime ProcessedAt { get; set; }
        public int ProcessingTimeMs { get; set; }
        public decimal LlmCost { get; set; }

        // Navigation properties
        public virtual CrawlJob? CrawlJob { get; set; }
    }
}
