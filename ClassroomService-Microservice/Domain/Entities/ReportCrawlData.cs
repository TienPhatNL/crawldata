using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Links crawl jobs from WebCrawlerService to reports in ClassroomService
/// Enables students to reference crawled data in their assignment submissions
/// </summary>
public class ReportCrawlData : BaseEntity
{
    /// <summary>
    /// Report this crawl data is linked to
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// External reference to CrawlJob in WebCrawlerService
    /// </summary>
    public Guid CrawlJobId { get; set; }

    /// <summary>
    /// Conversation ID from the crawler chat
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// AI-generated summary of the crawl data
    /// </summary>
    public string DataSummary { get; set; } = string.Empty;

    /// <summary>
    /// Source URL that was crawled
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Title or description of the crawled content
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// When this crawl data was linked to the report
    /// </summary>
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who linked this crawl data to the report
    /// </summary>
    public Guid LinkedBy { get; set; }

    /// <summary>
    /// Order/position of this crawl data in the report
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this crawl data is included in the final submission
    /// </summary>
    public bool IsIncludedInSubmission { get; set; } = true;

    /// <summary>
    /// Additional notes about how this data is used in the report
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Report Report { get; set; } = null!;
}
