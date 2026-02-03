using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Models;

public class CrawlJobStatusResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string[] Urls { get; set; } = Array.Empty<string>();
    public JobStatus Status { get; set; }
    public Priority Priority { get; set; }
    public CrawlerType CrawlerType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int UrlsProcessed { get; set; }
    public int UrlsSuccessful { get; set; }
    public int UrlsFailed { get; set; }
    public long TotalContentSize { get; set; }
    public int ProgressPercentage { get; set; }
}
