using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class UrlCrawlFailedEventHandler : INotificationHandler<UrlCrawlFailedEvent>
{
    private readonly ILogger<UrlCrawlFailedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public UrlCrawlFailedEventHandler(
        ILogger<UrlCrawlFailedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UrlCrawlFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("URL crawl failed for job {JobId}: {Url}. Error: {Error}, Retry: {RetryCount}, WillRetry: {WillRetry}",
            notification.JobId, notification.Url, notification.ErrorMessage,
            notification.RetryCount, notification.WillRetry);

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnUrlCrawlFailed", new
            {
                jobId = notification.JobId,
                url = notification.Url,
                errorMessage = notification.ErrorMessage,
                retryCount = notification.RetryCount,
                willRetry = notification.WillRetry,
                failedAt = notification.FailedAt
            }, cancellationToken);

        // Publish to Kafka for error tracking and analytics
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
