using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobExtractionEventHandler : INotificationHandler<JobExtractionEvent>
{
    private readonly ILogger<JobExtractionEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobExtractionEventHandler(
        ILogger<JobExtractionEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobExtractionEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extraction event {EventType} for job {JobId}: {Items} items from {Pages} pages ({Time}ms)",
            notification.ExtractionEventType, notification.JobId, 
            notification.TotalItemsExtracted, notification.PagesProcessed, notification.ExecutionTimeMs);

        // Broadcast to SignalR clients subscribed to this specific job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobExtraction", new
            {
                jobId = notification.JobId,
                extractionEventType = notification.ExtractionEventType,
                totalItemsExtracted = notification.TotalItemsExtracted,
                pagesProcessed = notification.PagesProcessed,
                extractionSuccessful = notification.ExtractionSuccessful,
                errorMessage = notification.ErrorMessage,
                executionTimeMs = notification.ExecutionTimeMs,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Broadcast to admin dashboard for monitoring
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobExtraction", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                extractionEventType = notification.ExtractionEventType,
                totalItemsExtracted = notification.TotalItemsExtracted,
                extractionSuccessful = notification.ExtractionSuccessful,
                occurredAt = notification.OccurredAt
            }, cancellationToken);

        // Publish to Kafka for other microservices
        await _eventPublisher.PublishAsync(notification, cancellationToken);
    }
}
