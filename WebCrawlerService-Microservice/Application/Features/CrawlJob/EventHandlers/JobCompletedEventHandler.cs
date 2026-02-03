using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Hubs;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Features.CrawlJob.EventHandlers;

public class JobCompletedEventHandler : INotificationHandler<JobCompletedEvent>
{
    private readonly ILogger<JobCompletedEventHandler> _logger;
    private readonly IHubContext<CrawlHub> _hubContext;
    private readonly IEventPublisher _eventPublisher;

    public JobCompletedEventHandler(
        ILogger<JobCompletedEventHandler> logger,
        IHubContext<CrawlHub> hubContext,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _hubContext = hubContext;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(JobCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Crawl job {JobId} completed for user {UserId}. " +
            "ConversationName: '{ConversationName}', " +
            "Processed: {UrlsProcessed}, Successful: {UrlsSuccessful}, Failed: {UrlsFailed}, " +
            "Total Size: {TotalContentSize} bytes, Duration: {Duration}ms",
            notification.JobId, notification.UserId, notification.ConversationName ?? "<null>",
            notification.UrlsProcessed, notification.UrlsSuccessful, notification.UrlsFailed, 
            notification.TotalContentSize, notification.ProcessingDuration.TotalMilliseconds);

        var successRate = notification.UrlsProcessed > 0
            ? (double)notification.UrlsSuccessful / notification.UrlsProcessed * 100
            : 0;

        // Broadcast to SignalR clients subscribed to this job
        await _hubContext.Clients
            .Group($"job_{notification.JobId}")
            .SendAsync("OnJobCompleted", new
            {
                jobId = notification.JobId,
                conversationName = notification.ConversationName,
                totalItems = notification.UrlsProcessed,
                successfulUrls = notification.UrlsSuccessful,
                failedUrls = notification.UrlsFailed,
                totalContentSize = notification.TotalContentSize,
                executionTimeMs = notification.ProcessingDuration.TotalMilliseconds,
                successRate = successRate,
                completedAt = notification.CompletedAt
            }, cancellationToken);

        // Broadcast to admin dashboard
        await _hubContext.Clients
            .Group("all_jobs")
            .SendAsync("OnJobCompleted", new
            {
                jobId = notification.JobId,
                userId = notification.UserId,
                conversationName = notification.ConversationName,
                totalItems = notification.UrlsProcessed,
                successRate = successRate,
                completedAt = notification.CompletedAt
            }, cancellationToken);

        // Update system metrics broadcast
        await _hubContext.Clients
            .Group("system_metrics")
            .SendAsync("OnJobCompleted", new
            {
                jobId = notification.JobId,
                timestamp = DateTime.UtcNow
            }, cancellationToken);

        // Publish to Kafka for other microservices
        await _eventPublisher.PublishAsync(notification, cancellationToken);

        // Broadcast chart-ready completion data
        await BroadcastCompletionChart(notification, successRate, cancellationToken);
    }

    private async Task BroadcastCompletionChart(JobCompletedEvent notification, double successRate, CancellationToken cancellationToken)
    {
        try
        {
            // Create ApexCharts-compatible radial bar data for final success rate
            var chartData = new
            {
                chart = new
                {
                    type = "radialBar",
                    title = "Job Completion",
                    subtitle = $"Job {notification.JobId.ToString()[..8]}... completed",
                    height = "300px"
                },
                data = new
                {
                    series = new[] { Math.Round(successRate, 2) },
                    labels = new[] { "Success Rate" },
                    current = notification.UrlsSuccessful,
                    target = notification.UrlsProcessed,
                    unit = "URLs"
                },
                colors = new[]
                {
                    successRate >= 80 ? "#00E396" :
                    successRate >= 50 ? "#FEB019" :
                    "#FF4560"
                },
                plotOptions = new
                {
                    radialBar = new
                    {
                        hollow = new { size = "70%" },
                        dataLabels = new
                        {
                            name = new { fontSize = "22px" },
                            value = new { fontSize = "16px" },
                            total = new
                            {
                                show = true,
                                label = $"{notification.UrlsSuccessful}/{notification.UrlsProcessed} URLs"
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
                        userId = notification.UserId,
                        conversationName = notification.ConversationName,
                        totalContentSize = notification.TotalContentSize,
                        executionTimeMs = notification.ProcessingDuration.TotalMilliseconds,
                        completedAt = notification.CompletedAt
                    }
                }
            };

            // Broadcast to clients subscribed to job charts
            await _hubContext.Clients
                .Group($"job_charts_{notification.JobId}")
                .SendAsync("OnJobCompletionChart", chartData, cancellationToken);

            // Also broadcast to system charts for dashboard updates
            await _hubContext.Clients
                .Group("system_charts")
                .SendAsync("OnJobCompletionChart", chartData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast completion chart for job {JobId}", notification.JobId);
        }
    }
}