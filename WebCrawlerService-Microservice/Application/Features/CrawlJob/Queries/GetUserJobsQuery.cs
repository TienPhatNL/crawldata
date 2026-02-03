using MediatR;
using WebCrawlerService.Application.Common.Behaviors;
using WebCrawlerService.Application.Common.Caching;
using WebCrawlerService.Domain.Common;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Features.CrawlJob.Queries;

public class GetUserJobsQuery : ICacheableRequest<PagedResult<CrawlJobSummaryResponse>>
{
    public Guid UserId { get; set; }
    public JobStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public string CacheKey => Status.HasValue 
        ? $"{CacheKeys.UserJobs(UserId, PageNumber, PageSize)}:status:{Status}"
        : CacheKeys.UserJobs(UserId, PageNumber, PageSize);
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
    public bool BypassCache { get; set; } = false;
}

public class CrawlJobSummaryResponse
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
    public long TotalContentSize { get; set; }
    public string? ErrorMessage { get; set; }
}