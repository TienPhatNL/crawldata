using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class CrawlerFailedEventHandler : INotificationHandler<CrawlerFailedEvent>
{
    private readonly ILogger<CrawlerFailedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public CrawlerFailedEventHandler(
        ILogger<CrawlerFailedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(CrawlerFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogError("Crawl job {JobId} failed for user {UserId}. " +
            "URL: {Url}, Error: {ErrorMessage}, Retry Count: {RetryCount}, Will Retry: {WillRetry}",
            notification.JobId, notification.UserId, notification.Url,
            notification.ErrorMessage, notification.RetryCount, notification.WillRetry);

        // Broadcast failure to SignalR clients
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobFailed", new
            {
                jobId = notification.JobId,
                errorMessage = notification.ErrorMessage,
                url = notification.Url,
                retryCount = notification.RetryCount,
                willRetry = notification.WillRetry,
                failedAt = notification.FailedAt
            }, cancellationToken);

        // Broadcast to admin dashboard
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobFailed", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                errorMessage = notification.ErrorMessage,
                failedAt = notification.FailedAt
            }, cancellationToken);

        // Publish to Kafka for other microservices and error tracking
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}