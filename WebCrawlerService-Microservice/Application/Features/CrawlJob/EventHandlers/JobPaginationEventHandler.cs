using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobPaginationEventHandler : INotificationHandler<JobPaginationEvent>
{
    private readonly ILogger<JobPaginationEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobPaginationEventHandler(
        ILogger<JobPaginationEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobPaginationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pagination: Job {JobId} loaded page {Page}/{MaxPages} ({TotalCollected} total, {Size} chars)",
            notification.JobId, notification.PageNumber, notification.MaxPages, 
            notification.TotalPagesCollected, notification.PageContentSize);

        // Broadcast to SignalR clients subscribed to this specific job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobPagination", new
            {
                jobId = notification.JobId,
                pageNumber = notification.PageNumber,
                totalPagesCollected = notification.TotalPagesCollected,
                maxPages = notification.MaxPages,
                pageContentSize = notification.PageContentSize,
                pageUrl = notification.PageUrl,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Broadcast to admin dashboard for monitoring
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobPagination", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                pageNumber = notification.PageNumber,
                totalPagesCollected = notification.TotalPagesCollected,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Publish to Kafka for other microservices
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
