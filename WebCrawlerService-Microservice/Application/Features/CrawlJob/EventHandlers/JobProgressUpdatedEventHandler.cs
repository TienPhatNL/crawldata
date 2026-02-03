using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobProgressUpdatedEventHandler : INotificationHandler<JobProgressUpdatedEvent>
{
    private readonly ILogger<JobProgressUpdatedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobProgressUpdatedEventHandler(
        ILogger<JobProgressUpdatedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobProgressUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job {JobId} progress: {Completed}/{Total} URLs ({Progress}%)",
            notification.JobId, notification.CompletedUrls, notification.TotalUrls,
            notification.ProgressPercentage);

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobProgress", new
            {
                jobId = notification.JobId,
                totalUrls = notification.TotalUrls,
                completedUrls = notification.CompletedUrls,
                failedUrls = notification.FailedUrls,
                progressPercentage = notification.ProgressPercentage,
                currentUrl = notification.CurrentUrl,
                updatedAt = notification.UpdatedAt
            }, cancellationToken);

        // Broadcast to admin dashboard for overall system monitoring
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobProgress", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                progressPercentage = notification.ProgressPercentage,
                updatedAt = notification.UpdatedAt
            }, cancellationToken);

        // Publish to Kafka for analytics
        await _eventPublisher.PublishAsync(notification, cancellationToken);

        // Broadcast chart-ready data for real-time visualization
        await BroadcastProgressChart(notification, cancellationToken);
    }

    private async Task BroadcastProgressChart(JobProgressUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Create ApexCharts-compatible radial bar data for live progress
            var chartData = new
            {
                chart = new
                {
                    type = "radialBar",
                    title = "Job Progress",
                    height = "300px"
                },
                data = new
                {
                    series = new[] { notification.ProgressPercentage },
                    labels = new[] { "Progress" },
                    current = notification.CompletedUrls,
                    target = notification.TotalUrls,
                    unit = "URLs"
                },
                colors = new[]
                {
                    notification.ProgressPercentage >= 100 ? "#00E396" :
                    notification.FailedUrls > notification.CompletedUrls / 2 ? "#FF4560" :
                    "#008FFB"
                },
                plotOptions = new
                {
                    radialBar = new
                    {
                        hollow = new { size = "65%" },
                        dataLabels = new
                        {
                            name = new { fontSize = "18px" },
                            value = new { fontSize = "14px" },
                            total = new
                            {
                                show = true,
                                label = $"{notification.CompletedUrls}/{notification.TotalUrls} URLs"
                            }
                        }
                    }
                },
                metadata = new
                {
                    generatedAt = DateTime.UtcNow,
                    dataPoints = 1,
                    additionalInfo = new
                    {
                        jobId = notification.JobId,
                        currentUrl = notification.CurrentUrl,
                        failedUrls = notification.FailedUrls
                    }
                }
            };

            // Broadcast to clients subscribed to job charts
            await _hubContext.Clients
                .Group($"job_charts_{notification.JobId}")
                .SendAsync("OnJobProgressChart", chartData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast progress chart for job {JobId}", notification.JobId);
        }
    }
}
