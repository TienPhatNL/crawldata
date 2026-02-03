using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobNavigationEventHandler : INotificationHandler<JobNavigationEvent>
{
    private readonly ILogger<JobNavigationEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobNavigationEventHandler(
        ILogger<JobNavigationEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobNavigationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Navigation event {EventType} for job {JobId} (Step {Step}/{Total})",
            notification.NavigationEventType, notification.JobId, notification.StepNumber, notification.TotalSteps);

        // Broadcast to SignalR clients subscribed to this specific job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobNavigation", new
            {
                jobId = notification.JobId,
                navigationEventType = notification.NavigationEventType,
                stepNumber = notification.StepNumber,
                totalSteps = notification.TotalSteps,
                action = notification.Action,
                description = notification.Description,
                currentUrl = notification.CurrentUrl,
                targetElement = notification.TargetElement,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Broadcast to admin dashboard for monitoring
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobNavigation", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                navigationEventType = notification.NavigationEventType,
                stepNumber = notification.StepNumber,
                totalSteps = notification.TotalSteps,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Publish to Kafka for other microservices
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
