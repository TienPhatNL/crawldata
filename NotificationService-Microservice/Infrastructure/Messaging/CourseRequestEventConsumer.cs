using Confluent.Kafka;
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

public class CourseRequestEventConsumer : BaseKafkaConsumer
{
    public CourseRequestEventConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<CourseRequestEventConsumer> logger)
        : base(kafkaSettings, serviceProvider, logger, "course-request")
    {
    }

    protected override string[] GetTopics()
    {
        return new[] { "classroom-events" };
    }

    protected override async Task HandleMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[CourseRequestEventConsumer] Received event from topic {Topic}", consumeResult.Topic);

            var eventType = GetEventType(consumeResult.Message.Headers);
            var eventJson = consumeResult.Message.Value;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var hubContextType = Type.GetType("NotificationService.Application.Hubs.NotificationHub, Application");
            var hubContextGenericType = typeof(IHubContext<>).MakeGenericType(hubContextType!);
            var hubContext = (IHubContext)scope.ServiceProvider.GetRequiredService(hubContextGenericType);

            switch (eventType)
            {
                case "CourseRequestCreatedEvent":
                    await HandleCourseRequestCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CourseRequestApprovedEvent":
                    await HandleCourseRequestApprovedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CourseRequestRejectedEvent":
                    await HandleCourseRequestRejectedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                default:
                    // Ignore other event types
                    return;
            }

            _logger.LogInformation("[CourseRequestEventConsumer] Processed event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CourseRequestEventConsumer] Error processing message");
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

    private async Task HandleCourseRequestCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseRequestCreatedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[CourseRequestEventConsumer] Processing CourseRequestCreatedEvent: {CourseCode} - {CourseTitle} by {LecturerName}",
            @event.CourseCode, @event.CourseTitle, @event.LecturerName);

        // Create group notification for all staff
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty, // Group notification for staff
            Title = "New Course Request 📋",
            Content = $"{@event.LecturerName} requested to create {@event.CourseCode} - {@event.CourseTitle} for {@event.Term} {@event.Year}",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "CourseRequestCreated",
                @event.CourseRequestId,
                @event.CourseCode,
                @event.CourseTitle,
                @event.LecturerId
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send to SupportStaff SignalR group
        await hubContext.Clients.Group("SupportStaff").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CourseRequestEventConsumer] Notified SupportStaff group about new course request");
    }

    private async Task HandleCourseRequestApprovedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseRequestApprovedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[CourseRequestEventConsumer] Processing CourseRequestApprovedEvent: {CourseName} approved for lecturer {LecturerId}",
            @event.CourseName, @event.LecturerId);

        // Create personal notification for lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Course Request Approved ✅",
            Content = $"Your course request for {@event.CourseCode} - {@event.CourseName} has been approved by {@event.StaffName}. Your course is now active!{(!string.IsNullOrEmpty(@event.ApprovalComments) ? $" Comments: {@event.ApprovalComments}" : "")}",
            Type = NotificationType.CourseApproved,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "CourseRequestApproved",
                @event.CourseRequestId,
                @event.CreatedCourseId,
                @event.CourseCode,
                @event.CourseName
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR to lecturer
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CourseRequestEventConsumer] Notified lecturer {LecturerId} about course request approval", @event.LecturerId);
    }

    private async Task HandleCourseRequestRejectedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseRequestRejectedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[CourseRequestEventConsumer] Processing CourseRequestRejectedEvent: {CourseCode} rejected for lecturer {LecturerId}",
            @event.CourseCode, @event.LecturerId);

        // Create personal notification for lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Course Request Rejected ❌",
            Content = $"Your course request for {@event.CourseCode} - {@event.CourseTitle} was rejected by {@event.StaffName}.{(!string.IsNullOrEmpty(@event.RejectionComments) ? $" Reason: {@event.RejectionComments}" : "")}",
            Type = NotificationType.CourseRejected,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "CourseRequestRejected",
                @event.CourseRequestId,
                @event.CourseCode,
                @event.CourseTitle
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR to lecturer
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[CourseRequestEventConsumer] Notified lecturer {LecturerId} about course request rejection", @event.LecturerId);
    }

    // DTOs for deserialization
    private class CourseRequestCreatedEventDto
    {
        public Guid CourseRequestId { get; set; }
        public Guid LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? RequestReason { get; set; }
    }

    private class CourseRequestApprovedEventDto
    {
        public Guid CourseRequestId { get; set; }
        public Guid CreatedCourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public Guid LecturerId { get; set; }
        public Guid ApprovedBy { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string? ApprovalComments { get; set; }
    }

    private class CourseRequestRejectedEventDto
    {
        public Guid CourseRequestId { get; set; }
        public Guid LecturerId { get; set; }
        public Guid RejectedBy { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string? RejectionComments { get; set; }
    }
}

