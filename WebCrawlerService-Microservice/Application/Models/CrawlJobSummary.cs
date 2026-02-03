using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Models;

public class CrawlJobSummary
{
    public Guid Id { get; set; }
    public Guid? AssignmentId { get; set; }
    public int UrlCount { get; set; }
    public JobStatus Status { get; set; }
    public Priority Priority { get; set; }
    public CrawlerType CrawlerType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int UrlsProcessed { get; set; }
    public int UrlsSuccessful { get; set; }
    public int UrlsFailed { get; set; }
}
