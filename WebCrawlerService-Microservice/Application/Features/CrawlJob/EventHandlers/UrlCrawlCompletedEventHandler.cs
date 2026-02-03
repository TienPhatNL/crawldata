using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class UrlCrawlCompletedEventHandler : INotificationHandler<UrlCrawlCompletedEvent>
{
    private readonly ILogger<UrlCrawlCompletedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public UrlCrawlCompletedEventHandler(
        ILogger<UrlCrawlCompletedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UrlCrawlCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("URL crawl completed for job {JobId}: {Url} (Status: {StatusCode}, ResponseTime: {ResponseTime}ms, Items: {ItemCount})",
            notification.JobId, notification.Url, notification.HttpStatusCode,
            notification.ResponseTimeMs, notification.ExtractedItemCount);

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnUrlCrawlCompleted", new
            {
                jobId = notification.JobId,
                url = notification.Url,
                statusCode = notification.HttpStatusCode,
                responseTimeMs = notification.ResponseTimeMs,
                extractedItemCount = notification.ExtractedItemCount,
                contentSize = notification.ContentSize,
                completedAt = notification.CompletedAt
            }, cancellationToken);

        // Publish to Kafka for analytics and monitoring
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
