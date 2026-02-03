using MediatR;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Features.CrawlJob.Commands;

public class StartCrawlJobCommand : IRequest<StartCrawlJobResponse>
{
    public Guid UserId { get; set; }
    public Guid? AssignmentId { get; set; }
    public required string[] Urls { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public CrawlerType CrawlerType { get; set; } = CrawlerType.HttpClient;
    public int TimeoutSeconds { get; set; } = 30;
    public bool FollowRedirects { get; set; } = true;
    public bool ExtractImages { get; set; } = false;
    public bool ExtractLinks { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public string ConfigurationJson { get; set; } = "{}";
}

public class StartCrawlJobResponse
{
    public Guid JobId { get; set; }
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}