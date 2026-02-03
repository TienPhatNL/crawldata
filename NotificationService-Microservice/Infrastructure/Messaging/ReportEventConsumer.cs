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

public class ReportEventConsumer : BaseKafkaConsumer
{
    public ReportEventConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<ReportEventConsumer> logger)
        : base(kafkaSettings, serviceProvider, logger, "report")
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
            _logger.LogInformation("[ReportEventConsumer] Received event from topic {Topic}", consumeResult.Topic);

            var eventType = GetEventType(consumeResult.Message.Headers);
            var eventJson = consumeResult.Message.Value;

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var hubContextType = Type.GetType("NotificationService.Application.Hubs.NotificationHub, Application");
            var hubContextGenericType = typeof(IHubContext<>).MakeGenericType(hubContextType!);
            var hubContext = (IHubContext)scope.ServiceProvider.GetRequiredService(hubContextGenericType);

            switch (eventType)
            {
                case "ReportSubmittedEvent":
                    await HandleReportSubmittedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "ReportGradedEvent":
                    await HandleReportGradedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "ReportRevisionRequestedEvent":
                    await HandleReportRevisionRequestedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "ReportResubmittedEvent":
                    await HandleReportResubmittedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "ReportContentRevertedEvent":
                    await HandleReportContentRevertedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                default:
                    // Ignore other event types
                    return;
            }

            _logger.LogInformation("[ReportEventConsumer] Processed event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReportEventConsumer] Error processing message");
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

    private async Task HandleReportSubmittedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ReportSubmittedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[ReportEventConsumer] Processing ReportSubmittedEvent: {AssignmentTitle} by {SubmitterName}",
            @event.AssignmentTitle, @event.SubmitterName);

        // Create personal notification for lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "New Report Submitted 📄",
            Content = $"{@event.SubmitterName} submitted {@event.AssignmentTitle} for {@event.CourseName}{(@event.IsGroupSubmission ? $" (Group: {@event.GroupName})" : "")}",
            Type = NotificationType.ReportSubmitted,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "ReportSubmitted",
                ReportId = @event.ReportId,
                AssignmentId = @event.AssignmentId,
                CourseId = @event.CourseId,
                GroupId = @event.GroupId,
                IsGroupSubmission = @event.IsGroupSubmission
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR to lecturer
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ReportEventConsumer] Notified lecturer {LecturerId} about report submission", @event.LecturerId);
    }

    private async Task HandleReportGradedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ReportGradedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[ReportEventConsumer] Processing ReportGradedEvent: {AssignmentTitle} graded with {Grade}",
            @event.AssignmentTitle, @event.Grade);

        // Create notifications for all students
        foreach (var studentId in @event.StudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = "Report Graded ⭐",
                Content = $"Your report for {@event.AssignmentTitle} has been graded: {(@event.Grade.HasValue ? $"{@event.Grade.Value:F2}/{(@event.MaxPoints.HasValue ? @event.MaxPoints.Value.ToString() : "100")}" : "Pass")}{(!string.IsNullOrEmpty(@event.Feedback) ? $". Feedback: {@event.Feedback}" : "")}",
                Type = NotificationType.ReportGraded,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Event = "ReportGraded",
                    ReportId = @event.ReportId,
                    AssignmentId = @event.AssignmentId,
                    CourseId = @event.CourseId,
                    GroupId = @event.GroupId,
                    Grade = @event.Grade,
                    MaxPoints = @event.MaxPoints,
                    IsGroupSubmission = @event.IsGroupSubmission
                })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send via SignalR to student
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("[ReportEventConsumer] Notified student {StudentId} about report grade", studentId);
        }
    }

    private async Task HandleReportRevisionRequestedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ReportRevisionRequestedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[ReportEventConsumer] Processing ReportRevisionRequestedEvent: {AssignmentTitle}",
            @event.AssignmentTitle);

        // Create notifications for all students
        foreach (var studentId in @event.StudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = "Revision Required 🔄",
                Content = $"{@event.LecturerName} requested revisions for your report: {@event.AssignmentTitle}.{(!string.IsNullOrEmpty(@event.Feedback) ? $" Feedback: {@event.Feedback}" : "")}",
                Type = NotificationType.RevisionRequested,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Event = "ReportRevisionRequested",
                    ReportId = @event.ReportId,
                    AssignmentId = @event.AssignmentId,
                    CourseId = @event.CourseId,
                    GroupId = @event.GroupId,
                    IsGroupSubmission = @event.IsGroupSubmission
                })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send via SignalR to student
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("[ReportEventConsumer] Notified student {StudentId} about revision request", studentId);
        }
    }

    private async Task HandleReportResubmittedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ReportResubmittedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[ReportEventConsumer] Processing ReportResubmittedEvent: {AssignmentTitle} (Version {Version}) by {SubmitterName}",
            @event.AssignmentTitle, @event.Version, @event.SubmitterName);

        // Create personal notification for lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Report Resubmitted 🔄",
            Content = $"{@event.SubmitterName} resubmitted {@event.AssignmentTitle} (Version {@event.Version})",
            Type = NotificationType.ReportSubmitted,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "ReportResubmitted",
                ReportId = @event.ReportId,
                AssignmentId = @event.AssignmentId,
                CourseId = @event.CourseId,
                GroupId = @event.GroupId,
                Version = @event.Version
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR to lecturer
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ReportEventConsumer] Notified lecturer {LecturerId} about report resubmission", @event.LecturerId);
    }

    // DTOs for deserialization
    private class ReportSubmittedEventDto
    {
        public Guid ReportId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public Guid SubmittedBy { get; set; }
        public string SubmitterName { get; set; } = string.Empty;
        public bool IsGroupSubmission { get; set; }
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
        public Guid LecturerId { get; set; }
    }

    private class ReportGradedEventDto
    {
        public Guid ReportId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public decimal? Grade { get; set; }
        public int? MaxPoints { get; set; }
        public string? Feedback { get; set; }
        public Guid GradedBy { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public List<Guid> StudentIds { get; set; } = new();
        public bool IsGroupSubmission { get; set; }
        public Guid? GroupId { get; set; }
    }

    private class ReportRevisionRequestedEventDto
    {
        public Guid ReportId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string? Feedback { get; set; }
        public Guid RequestedBy { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public List<Guid> StudentIds { get; set; } = new();
        public bool IsGroupSubmission { get; set; }
        public Guid? GroupId { get; set; }
    }

    private class ReportResubmittedEventDto
    {
        public Guid ReportId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public Guid SubmittedBy { get; set; }
        public string SubmitterName { get; set; } = string.Empty;
        public int Version { get; set; }
        public Guid? GroupId { get; set; }
        public Guid LecturerId { get; set; }
    }

    private async Task HandleReportContentRevertedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ReportContentRevertedEventDto>(eventJson, _jsonOptions);
        if (@event == null) return;

        _logger.LogInformation("[ReportEventConsumer] Processing ReportContentRevertedEvent: {AssignmentTitle} reverted by {RevertedByName}",
            @event.AssignmentTitle, @event.RevertedByName);

        // Notify all group members except the one who reverted
        foreach (var memberId in @event.GroupMemberIds.Where(id => id != @event.RevertedBy))
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = "Group Report Content Reverted 🔄",
                Content = $"{@event.RevertedByName} reverted {@event.GroupName}'s {@event.AssignmentTitle} content to version {@event.RevertedToVersion}{(!string.IsNullOrEmpty(@event.Comment) ? $": {@event.Comment}" : "")}",
                Type = NotificationType.ReportReverted,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Event = "ReportContentReverted",
                    ReportId = @event.ReportId,
                    AssignmentId = @event.AssignmentId,
                    CourseId = @event.CourseId,
                    GroupId = @event.GroupId,
                    RevertedToVersion = @event.RevertedToVersion
                })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            
            // Send via SignalR to group member
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[ReportEventConsumer] Notified {MemberCount} group members about report content revert",
            @event.GroupMemberIds.Count - 1);
    }

    private class ReportContentRevertedEventDto
    {
        public Guid ReportId { get; set; }
        public Guid CourseId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid RevertedBy { get; set; }
        public string RevertedByName { get; set; } = string.Empty;
        public int RevertedToVersion { get; set; }
        public List<Guid> GroupMemberIds { get; set; } = new();
        public string? Comment { get; set; }
    }
}

