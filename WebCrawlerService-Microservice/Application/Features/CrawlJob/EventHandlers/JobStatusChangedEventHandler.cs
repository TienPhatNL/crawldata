using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobStatusChangedEventHandler : INotificationHandler<JobStatusChangedEvent>
{
    private readonly ILogger<JobStatusChangedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobStatusChangedEventHandler(
        ILogger<JobStatusChangedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobId} status changed: {OldStatus} → {NewStatus}",
            notification.JobId, notification.OldStatus, notification.NewStatus);

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobStatusChanged", new
            {
                jobId = notification.JobId,
                oldStatus = notification.OldStatus.ToString(),
                newStatus = notification.NewStatus.ToString(),
                changedAt = notification.ChangedAt
            }, cancellationToken);

        // Broadcast to admin dashboard
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobStatusChanged", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                newStatus = notification.NewStatus.ToString(),
                changedAt = notification.ChangedAt
            }, cancellationToken);

        // Publish to Kafka
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
