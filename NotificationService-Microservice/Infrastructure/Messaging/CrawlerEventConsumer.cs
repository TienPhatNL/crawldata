using Confluent.Kafka;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Domain.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System.Text.Json;

namespace NotificationService.Infrastructure.Messaging;

public class CrawlerEventConsumer : BaseKafkaConsumer
{
    public CrawlerEventConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<CrawlerEventConsumer> logger)
        : base(kafkaSettings, serviceProvider, logger, "crawler")
    {
    }

    protected override string[] GetTopics()
    {
        return new[] { "crawler-events" };
    }

    protected override async Task HandleMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[CrawlerEventConsumer] Received crawler event from topic {Topic}", consumeResult.Topic);

            var eventType = GetEventType(consumeResult.Message.Headers);
            var eventJson = consumeResult.Message.Value;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // Get the hub context dynamically to avoid circular dependency between Infrastructure and Application
            var hubContextType = Type.GetType("NotificationService.Application.Hubs.NotificationHub, Application");
            var hubContextGenericType = typeof(IHubContext<>).MakeGenericType(hubContextType!);
            var hubContext = (IHubContext)scope.ServiceProvider.GetRequiredService(hubContextGenericType);

            switch (eventType)
            {
                case "JobStartedEvent":
                    await HandleJobStartedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "JobCompletedEvent":
                    await HandleJobCompletedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CrawlerFailedEvent":
                    await HandleCrawlerFailedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "JobProgressUpdatedEvent":
                    await HandleJobProgressUpdatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UrlCrawlCompletedEvent":
                    await HandleUrlCrawlCompletedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "UrlCrawlFailedEvent":
                    await HandleUrlCrawlFailedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "JobStatusChangedEvent":
                    await HandleJobStatusChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("[CrawlerEventConsumer] Unknown event type: {EventType}", eventType);
                    break;
            }

            _logger.LogInformation("[CrawlerEventConsumer] Processed event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CrawlerEventConsumer] Error handling crawler event");
            throw;
        }
    }

    private string? GetEventType(Headers headers)
    {
        var eventTypeHeader = headers.FirstOrDefault(h => h.Key == "event-type");
        if (eventTypeHeader != null)
        {
            return System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
        }
        return null;
    }

    private async Task HandleJobStartedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<JobStartedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Crawl Job Started 🚀",
            Content = $"Your crawl job has started processing {(@event.Urls?.Length ?? 0)} URL(s).",
            Type = NotificationType.CrawlJobStarted,
            Priority = NotificationPriority.Low,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created job-started notification for user {UserId}", @event.UserId);
    }

    private async Task HandleJobCompletedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<JobCompletedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Crawl Job Completed ✅",
            Content = $"Your crawl job completed successfully. Processed {@event.UrlsProcessed} URLs ({@event.UrlsSuccessful} successful, {@event.UrlsFailed} failed).",
            Type = NotificationType.CrawlJobCompleted,
            Priority = NotificationPriority.Normal,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "JobCompleted", 
                JobId = @event.JobId, 
                UrlsProcessed = @event.UrlsProcessed,
                UrlsSuccessful = @event.UrlsSuccessful,
                UrlsFailed = @event.UrlsFailed,
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created job-completed notification for user {UserId}", @event.UserId);
    }

    private async Task HandleCrawlerFailedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CrawlerFailedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Crawl Job Failed ❌",
            Content = $"Your crawl job encountered an error: {@event.ErrorMessage}",
            Type = NotificationType.CrawlJobFailed,
            Priority = NotificationPriority.High,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "CrawlerFailed", 
                JobId = @event.JobId, 
                ErrorMessage = @event.ErrorMessage,
                Url = @event.Url,
                RetryCount = @event.RetryCount,
                WillRetry = @event.WillRetry,
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created crawler-failed notification for user {UserId}", @event.UserId);
    }

    private async Task HandleJobProgressUpdatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<JobProgressUpdatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Only create notification for major milestones (e.g., 50%, 100%)
        if (@event.ProgressPercentage < 50 && @event.ProgressPercentage != 100)
        {
            _logger.LogDebug("[CrawlerEventConsumer] Skipping progress notification for {ProgressPercentage}%", @event.ProgressPercentage);
            return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "Crawl Job Progress Update 📊",
            Content = $"Your crawl job is {@event.ProgressPercentage:F1}% complete. {@event.CompletedUrls}/{@event.TotalUrls} URLs processed.",
            Type = NotificationType.CrawlJobProgress,
            Priority = NotificationPriority.Low,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "JobProgress", 
                JobId = @event.JobId, 
                ProgressPercentage = @event.ProgressPercentage,
                CompletedUrls = @event.CompletedUrls,
                FailedUrls = @event.FailedUrls,
                TotalUrls = @event.TotalUrls,
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created job-progress notification for user {UserId}", @event.UserId);
    }

    private async Task HandleUrlCrawlCompletedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UrlCrawlCompletedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Don't create individual URL notifications - too granular
        // These are logged but not sent to users unless they specifically subscribe to URL-level updates
        _logger.LogDebug("[CrawlerEventConsumer] URL crawl completed: {Url} for job {JobId}", @event.Url, @event.JobId);
        await Task.CompletedTask;
    }

    private async Task HandleUrlCrawlFailedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<UrlCrawlFailedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Only notify on URL failures if it's a critical error
        if (@event.WillRetry)
        {
            _logger.LogDebug("[CrawlerEventConsumer] URL crawl failed but will retry: {Url}", @event.Url);
            return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = "URL Crawl Failed ⚠️",
            Content = $"Failed to crawl {@event.Url}: {@event.ErrorMessage}",
            Type = NotificationType.UrlCrawlFailed,
            Priority = NotificationPriority.Normal,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "UrlCrawlFailed", 
                JobId = @event.JobId, 
                Url = @event.Url,
                ErrorMessage = @event.ErrorMessage,
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created url-crawl-failed notification for user {UserId}", @event.UserId);
    }

    private async Task HandleJobStatusChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<JobStatusChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Only notify on significant status changes (e.g., Queued → Running, Paused, Cancelled)
        if (@event.NewStatus == "Queued" || @event.NewStatus == "Pending")
        {
            _logger.LogDebug("[CrawlerEventConsumer] Skipping notification for status: {NewStatus}", @event.NewStatus);
            return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.UserId,
            Title = $"Crawl Job Status: {@event.NewStatus} 🔄",
            Content = $"Your crawl job status changed from {@event.OldStatus} to {@event.NewStatus}.",
            Type = NotificationType.CrawlJobStatusChanged,
            Priority = @event.NewStatus == "Cancelled" ? NotificationPriority.High : NotificationPriority.Normal,
            Source = EventSource.WebCrawlerService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new 
            { 
                Event = "JobStatusChanged", 
                JobId = @event.JobId, 
                OldStatus = @event.OldStatus,
                NewStatus = @event.NewStatus,
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CrawlerEventConsumer] Created job-status-changed notification for user {UserId}", @event.UserId);
    }
}

// DTOs for deserializing crawler events - matching WebCrawlerService domain events
public class JobStartedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string[] Urls { get; set; } = Array.Empty<string>();
    public string? CrawlerType { get; set; }
    public string? Priority { get; set; }
    public DateTime StartedAt { get; set; }
}

public class JobCompletedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CompletedAt { get; set; }
    public int UrlsProcessed { get; set; }
    public int UrlsSuccessful { get; set; }
    public int UrlsFailed { get; set; }
    public long TotalContentSize { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}

public class CrawlerFailedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int RetryCount { get; set; }
    public bool WillRetry { get; set; }
}

public class JobProgressUpdatedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public int TotalUrls { get; set; }
    public int CompletedUrls { get; set; }
    public int FailedUrls { get; set; }
    public double ProgressPercentage { get; set; }
    public string? CurrentUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UrlCrawlCompletedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class UrlCrawlFailedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public bool WillRetry { get; set; }
    public DateTime FailedAt { get; set; }
}

public class JobStatusChangedEventDto
{
    public Guid JobId { get; set; }
    public Guid UserId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}


