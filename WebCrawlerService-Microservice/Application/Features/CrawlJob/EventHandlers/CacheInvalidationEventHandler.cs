using MediatR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Caching;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Domain.Events;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class CacheInvalidationEventHandler : 
    INotificationHandler<JobStartedEvent>,
    INotificationHandler<JobCompletedEvent>,
    INotificationHandler<CrawlerFailedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationEventHandler> _logger;

    public CacheInvalidationEventHandler(ICacheService cacheService, ILogger<CacheInvalidationEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(JobStartedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateCrawlJobCaches(notification.JobId, notification.UserId, cancellationToken);
        _logger.LogDebug("Cache invalidated for job started: {JobId}", notification.JobId);
    }

    public async Task Handle(JobCompletedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateCrawlJobCaches(notification.JobId, notification.UserId, cancellationToken);
        _logger.LogDebug("Cache invalidated for job completed: {JobId}", notification.JobId);
    }

    public async Task Handle(CrawlerFailedEvent notification, CancellationToken cancellationToken)
    {
        await InvalidateCrawlJobCaches(notification.JobId, notification.UserId, cancellationToken);
        _logger.LogDebug("Cache invalidated for job failed: {JobId}", notification.JobId);
    }

    private async Task InvalidateCrawlJobCaches(Guid jobId, Guid userId, CancellationToken cancellationToken)
    {
        // Invalidate specific job cache
        await _cacheService.RemoveAsync(CacheKeys.CrawlJob(jobId), cancellationToken);
        
        // Invalidate user jobs cache patterns (all pages and filters)
        await _cacheService.RemoveByPatternAsync(CacheKeys.UserJobsPattern(userId), cancellationToken);
        
        // Invalidate crawl results cache
        await _cacheService.RemoveAsync(CacheKeys.CrawlResults(jobId), cancellationToken);
    }
}