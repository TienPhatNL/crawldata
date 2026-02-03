using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobStartedEventHandler : INotificationHandler<JobStartedEvent>
{
    private readonly ILogger<JobStartedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobStartedEventHandler(
        ILogger<JobStartedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Crawl job {JobId} started for user {UserId} with {UrlCount} URLs",
            notification.JobId, notification.UserId, notification.Urls.Length);

        // Broadcast to SignalR clients subscribed to this specific job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobStarted", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                totalUrls = notification.Urls.Length,
                crawlerType = notification.CrawlerType.ToString(),
                priority = notification.Priority.ToString(),
                startedAt = notification.StartedAt
            }, cancellationToken);

        // Broadcast to admin dashboard (all jobs monitoring)
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobStarted", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                totalUrls = notification.Urls.Length,
                crawlerType = notification.CrawlerType.ToString(),
                startedAt = notification.StartedAt
            }, cancellationToken);

        // Publish to Kafka for other microservices
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}