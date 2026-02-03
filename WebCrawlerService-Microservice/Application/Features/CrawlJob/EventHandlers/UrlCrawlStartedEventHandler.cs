using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class UrlCrawlStartedEventHandler : INotificationHandler<UrlCrawlStartedEvent>
{
    private readonly ILogger<UrlCrawlStartedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public UrlCrawlStartedEventHandler(
        ILogger<UrlCrawlStartedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(UrlCrawlStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("URL crawl started for job {JobId}: {Url}",
            notification.JobId, notification.Url);

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnUrlCrawlStarted", new
            {
                jobId = notification.JobId,
                url = notification.Url,
                startedAt = notification.StartedAt
            }, cancellationToken);

        // Publish to Kafka (optional - may be too granular for some use cases)
        // await _eventPublisher.PublishAsync("crawler.url.started", notification, cancellationToken);
    }
}
