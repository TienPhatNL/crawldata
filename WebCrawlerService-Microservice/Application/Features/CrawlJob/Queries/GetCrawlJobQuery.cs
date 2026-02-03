using MediatR;
using WebCrawlerService.Application.Common.Behaviors;
using WebCrawlerService.Application.Common.Caching;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Features.CrawlJob.Queries;

public class GetCrawlJobQuery : ICacheableRequest<CrawlJobResponse?>
{
    public Guid JobId { get; set; }
    public Guid? UserId { get; set; } // For authorization check
    
    public string CacheKey => CacheKeys.CrawlJob(JobId);
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
    public bool BypassCache { get; set; } = false;
}

public class CrawlJobResponse
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
    public DateTime? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public int UrlsProcessed { get; set; }
    public int UrlsSuccessful { get; set; }
    public int UrlsFailed { get; set; }
    public long TotalContentSize { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool FollowRedirects { get; set; }
    public bool ExtractImages { get; set; }
    public bool ExtractLinks { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public CrawlerAgentResponse? AssignedAgent { get; set; }
    public List<CrawlResultResponse> Results { get; set; } = new();
}

public class CrawlerAgentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CrawlerType Type { get; set; }
    public AgentStatus Status { get; set; }
}

public class CrawlResultResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public long ContentSize { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CrawledAt { get; set; }
    public string[] Images { get; set; } = Array.Empty<string>();
    public string[] Links { get; set; } = Array.Empty<string>();
}