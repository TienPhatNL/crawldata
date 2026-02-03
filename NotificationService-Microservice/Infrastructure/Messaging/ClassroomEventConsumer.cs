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

public class ClassroomEventConsumer : BaseKafkaConsumer
{
    public ClassroomEventConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<ClassroomEventConsumer> logger)
        : base(kafkaSettings, serviceProvider, logger, "classroom")
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
            _logger.LogInformation("[ClassroomEventConsumer] Received classroom event from topic {Topic}", consumeResult.Topic);

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
                case "CourseCreatedEvent":
                    await HandleCourseCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CourseApprovedEvent":
                    await HandleCourseApprovedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CourseRejectedEvent":
                    await HandleCourseRejectedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "CourseStatusChangedEvent":
                    await HandleCourseStatusChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "StudentEnrolledEvent":
                    await HandleStudentEnrolledEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentCreatedEvent":
                    await HandleAssignmentCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentUpdatedEvent":
                    await HandleAssignmentUpdatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentDeletedEvent":
                    await HandleAssignmentDeletedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentClosedEvent":
                    await HandleAssignmentClosedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentDueDateExtendedEvent":
                    await HandleAssignmentDueDateExtendedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "AssignmentStatusChangedEvent":
                    await HandleAssignmentStatusChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupCreatedEvent":
                    await HandleGroupCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupMemberAddedEvent":
                    await HandleGroupMemberAddedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupMemberRemovedEvent":
                    await HandleGroupMemberRemovedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupLeaderChangedEvent":
                    await HandleGroupLeaderChangedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupAssignmentAssignedEvent":
                    await HandleGroupAssignmentAssignedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupsAssignedToAssignmentEvent":
                    await HandleGroupsAssignedToAssignmentEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "GroupsUnassignedFromAssignmentEvent":
                    await HandleGroupsUnassignedFromAssignmentEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SupportRequestCreatedEvent":
                    await HandleSupportRequestCreatedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SupportRequestAcceptedEvent":
                    await HandleSupportRequestAcceptedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SupportRequestRejectedEvent":
                    await HandleSupportRequestRejectedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SupportRequestResolvedEvent":
                    await HandleSupportRequestResolvedEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                case "SupportRequestCancelledEvent":
                    await HandleSupportRequestCancelledEvent(eventJson, unitOfWork, hubContext, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("[ClassroomEventConsumer] Unknown event type: {EventType}", eventType);
                    break;
            }

            _logger.LogInformation("[ClassroomEventConsumer] Processed event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClassroomEventConsumer] Error handling classroom event");
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

    private async Task HandleStudentEnrolledEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<StudentEnrolledEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Notify the student about enrollment
        var studentNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.StudentId,
            Title = "Enrolled in Course ✅",
            Content = $"You have been enrolled in '{@event.CourseName}'.",
            Type = NotificationType.EnrollmentApproved,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "StudentEnrolled", EnrollmentId = @event.EnrollmentId, CourseId = @event.CourseId })
        };

        await unitOfWork.Notifications.AddAsync(studentNotification);
        await hubContext.Clients.Group($"user_{@event.StudentId}").SendAsync("ReceiveNotification", studentNotification, cancellationToken);

        // Notify the lecturer about new enrollment
        var lecturerNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "New Student Enrolled 👥",
            Content = $"A new student has enrolled in '{@event.CourseName}'.",
            Type = NotificationType.EnrollmentApproved,
            Priority = NotificationPriority.Low,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "StudentEnrolled", StudentId = @event.StudentId, EnrollmentId = @event.EnrollmentId, CourseId = @event.CourseId })
        };

        await unitOfWork.Notifications.AddAsync(lecturerNotification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", lecturerNotification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created enrollment notifications for student {StudentId} and lecturer {LecturerId}", 
            @event.StudentId, @event.LecturerId);
    }

    private async Task HandleAssignmentCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentCreatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Assignment Created - only notify the lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Assignment Created Successfully 📚",
            Content = $"Your assignment '{@event.Title}' has been created (Due: {@event.DueDate:MMM dd, yyyy}).",
            Type = NotificationType.AssignmentCreated,
            Priority = NotificationPriority.Low,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentCreated", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created assignment-created notification for lecturer {LecturerId}", @event.LecturerId);
    }

    private async Task HandleAssignmentSubmittedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentSubmittedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Notify ALL enrolled students about submission
        foreach (var studentId in @event.EnrolledStudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = studentId == @event.SubmitterId ? "Assignment Submitted ✉️" : "Student Submitted Assignment 📄",
                Content = studentId == @event.SubmitterId 
                    ? $"Your submission for '{@event.AssignmentTitle}' has been received."
                    : $"A student submitted '{@event.AssignmentTitle}'.",
                Type = NotificationType.AssignmentCreated,
                Priority = studentId == @event.SubmitterId ? NotificationPriority.Low : NotificationPriority.Low,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentSubmitted", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId, SubmitterId = @event.SubmitterId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created assignment-submitted notifications for {Count} enrolled students", @event.EnrolledStudentIds.Count);
    }

    private async Task HandleAssignmentGradedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentGradedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Notify the student(s) who received the grade
        foreach (var studentId in @event.StudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = "Assignment Graded 📝",
                Content = @event.IsGroupSubmission
                    ? $"Your group assignment '{@event.AssignmentTitle}' was graded by {(@event.LecturerName ?? "your instructor")}. Grade: {@event.Grade}."
                    : $"Your assignment '{@event.AssignmentTitle}' was graded by {(@event.LecturerName ?? "your instructor")}. Grade: {@event.Grade}.",
                Type = NotificationType.AssignmentGraded,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentGraded", ReportId = @event.ReportId, AssignmentId = @event.AssignmentId, CourseId = @event.CourseId, Grade = @event.Grade, GradedBy = @event.GradedBy, IsGroupSubmission = @event.IsGroupSubmission })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created assignment-graded notifications for {Count} student(s)", @event.StudentIds.Count);
    }

    // ============ COURSE EVENTS ============
    private async Task HandleCourseCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseCreatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Create personal notification for lecturer
        var lecturerNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Course Created 📚",
            Content = $"You have successfully created the course: {@event.CourseName} ({@event.CourseCode})",
            Type = NotificationType.CourseUpdate,
            Priority = NotificationPriority.Low,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "CourseCreated", CourseId = @event.CourseId, CourseCode = @event.CourseCode })
        };

        await unitOfWork.Notifications.AddAsync(lecturerNotification);
        
        // Create group notification for staff
        var staffNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty, // Group notification for staff
            Title = "New Course Created 📚",
            Content = $"A new course has been created: {@event.CourseName} ({@event.CourseCode})",
            Type = NotificationType.CourseUpdate,
            Priority = NotificationPriority.Low,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "CourseCreated", CourseId = @event.CourseId, CourseCode = @event.CourseCode, LecturerId = @event.LecturerId })
        };

        await unitOfWork.Notifications.AddAsync(staffNotification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Send to lecturer's SignalR group
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", lecturerNotification, cancellationToken);
        
        // Send to staff's SignalR group
        await hubContext.Clients.Group("SupportStaff").SendAsync("ReceiveNotification", staffNotification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created course created notifications for lecturer {LecturerId} and staff", @event.LecturerId);
    }

    private async Task HandleCourseApprovedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseApprovedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Course Approved ✅",
            Content = $"Your course '{@event.CourseName}' has been approved and is now published!",
            Type = NotificationType.CourseApproved,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "CourseApproved", CourseId = @event.CourseId, ApprovedBy = @event.ApprovedBy })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("[ClassroomEventConsumer] Sending CourseApproved notification via SignalR to user_{LecturerId}", @event.LecturerId);
        try
        {
            await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("[ClassroomEventConsumer] Successfully sent CourseApproved notification via SignalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClassroomEventConsumer] Failed to send CourseApproved notification via SignalR");
        }

        _logger.LogInformation("[ClassroomEventConsumer] Created course approved notification for lecturer {LecturerId}", @event.LecturerId);
    }

    private async Task HandleCourseRejectedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseRejectedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Course Rejected ❌",
            Content = $"Your course '{@event.CourseName}' was rejected. Reason: {@event.Reason}",
            Type = NotificationType.CourseRejected,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "CourseRejected", CourseId = @event.CourseId, Reason = @event.Reason, RejectedBy = @event.RejectedBy })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created course rejected notification for lecturer {LecturerId}", @event.LecturerId);
    }

    private async Task HandleCourseStatusChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CourseStatusChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Map enum values to display names
        // CourseStatus: PendingApproval=1, Active=2, Inactive=3, Rejected=4
        var statusNames = new Dictionary<int, string>
        {
            { 1, "Pending Approval" },
            { 2, "Active" },
            { 3, "Inactive" },
            { 4, "Rejected" }
        };

        var oldStatusName = statusNames.GetValueOrDefault(@event.OldStatus, "Unknown");
        var newStatusName = statusNames.GetValueOrDefault(@event.NewStatus, "Unknown");

        // Notify on all status changes
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = $"Course Status Changed: {newStatusName} 🔄",
            Content = $"The status of '{@event.CourseName}' has changed from {oldStatusName} to {newStatusName}.{(@event.Comments != null ? $" Reason: {@event.Comments}" : "")}",
            Type = NotificationType.CourseUpdate,
            Priority = @event.NewStatus == 3 ? NotificationPriority.High : NotificationPriority.Normal, // High priority for Inactive
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "CourseStatusChanged", CourseId = @event.CourseId, OldStatus = oldStatusName, NewStatus = newStatusName })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("[ClassroomEventConsumer] Sending CourseStatusChanged notification via SignalR to user_{LecturerId}", @event.LecturerId);
        try
        {
            await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("[ClassroomEventConsumer] Successfully sent CourseStatusChanged notification via SignalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClassroomEventConsumer] Failed to send CourseStatusChanged notification via SignalR");
        }

        _logger.LogInformation("[ClassroomEventConsumer] Created course status changed notification for lecturer {LecturerId}: {OldStatus} -> {NewStatus}", 
            @event.LecturerId, oldStatusName, newStatusName);
    }

    // ============ ASSIGNMENT EVENTS (Additional) ============
    private async Task HandleAssignmentUpdatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentUpdatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Assignment Updated only notifies the lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Assignment Updated ✏️",
            Content = $"The assignment '{@event.Title}' has been updated.",
            Type = NotificationType.AssignmentCreated,
            Priority = NotificationPriority.Low,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentUpdated", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created assignment updated notification for lecturer {LecturerId}", @event.LecturerId);
    }

    private async Task HandleAssignmentDeletedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentDeletedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Assignment Deleted only notifies the lecturer
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Assignment Deleted 🗑️",
            Content = $"The assignment '{@event.Title}' has been deleted.",
            Type = NotificationType.AssignmentCreated,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentDeleted", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created assignment deleted notification for lecturer {LecturerId}", @event.LecturerId);
    }

    private async Task HandleAssignmentClosedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentClosedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Assignment Closed notifies all enrolled students
        foreach (var studentId in @event.EnrolledStudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = "Assignment Closed 🔒",
                Content = $"The assignment '{@event.Title}' has been closed. No more submissions will be accepted.",
                Type = NotificationType.AssignmentCreated,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentClosed", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} assignment closed notifications", @event.EnrolledStudentIds.Count);
    }

    private async Task HandleAssignmentDueDateExtendedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentDueDateExtendedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Assignment Due Date Extended notifies all enrolled students
        foreach (var studentId in @event.EnrolledStudentIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = studentId,
                Title = "Assignment Due Date Extended ⏰",
                Content = $"The due date for '{@event.Title}' has been extended to {@event.ExtendedDueDate:MMM dd, yyyy}",
                Type = NotificationType.AssignmentDueReminder,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "DueDateExtended", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId, OriginalDueDate = @event.OriginalDueDate, ExtendedDueDate = @event.ExtendedDueDate })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{studentId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} due date extension notifications", @event.EnrolledStudentIds.Count);
    }

    private async Task HandleAssignmentStatusChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<AssignmentStatusChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing AssignmentStatusChangedEvent: AssignmentId={AssignmentId}, Title={Title}, OldStatus={OldStatus}, NewStatus={NewStatus}", 
            @event.AssignmentId, @event.Title, @event.OldStatus, @event.NewStatus);

        // Business Rule: Assignment Status Changed only notifies the lecturer
        string statusText = @event.NewStatus switch
        {
            1 => "Draft",
            2 => "Scheduled",
            3 => "Active",
            4 => "Extended",
            5 => "Overdue",
            6 => "Closed",
            7 => "Graded",
            _ => "Unknown"
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.LecturerId,
            Title = "Assignment Status Changed 🔄",
            Content = $"The assignment '{@event.Title}' status has been changed to {statusText}.",
            Type = NotificationType.AssignmentCreated,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "AssignmentStatusChanged", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId, OldStatus = @event.OldStatus, NewStatus = @event.NewStatus })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.Group($"user_{@event.LecturerId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Created assignment status changed notification for lecturer {LecturerId}: '{Title}' -> {Status}", 
            @event.LecturerId, @event.Title, statusText);
    }

    // ============ GROUP EVENTS ============
    private async Task HandleGroupCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupCreatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Group Created notifies all group members
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = memberId == @event.CreatedBy ? "Group Created 👥" : "Added to New Group 🎉",
                Content = memberId == @event.CreatedBy 
                    ? $"You have created the group '{@event.GroupName}'." 
                    : $"You have been added to the group '{@event.GroupName}'.",
                Type = NotificationType.GroupInvitation,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupCreated", GroupId = @event.GroupId, CourseId = @event.CourseId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} group created notifications", @event.GroupMemberIds.Count);
    }

    private async Task HandleGroupMemberAddedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupMemberAddedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Group Member Added notifies all group members (including the new member)
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = memberId == @event.StudentId ? "Added to Group 🎉" : "New Group Member 👤",
                Content = memberId == @event.StudentId 
                    ? $"You have been added to the group '{@event.GroupName}'." 
                    : $"A new member has been added to the group '{@event.GroupName}'.",
                Type = NotificationType.GroupMemberAdded,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupMemberAdded", GroupId = @event.GroupId, CourseId = @event.CourseId, StudentId = @event.StudentId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} group member added notifications", @event.GroupMemberIds.Count);
    }

    private async Task HandleGroupMemberRemovedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupMemberRemovedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Group Member Removed notifies the removed member and all remaining members
        // First notify the removed member
        var removedNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.StudentId,
            Title = "Removed from Group ⚠️",
            Content = $"You have been removed from the group '{@event.GroupName}'.",
            Type = NotificationType.GroupMemberRemoved,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { Event = "GroupMemberRemoved", GroupId = @event.GroupId, CourseId = @event.CourseId, StudentId = @event.StudentId })
        };

        await unitOfWork.Notifications.AddAsync(removedNotification);
        await hubContext.Clients.Group($"user_{@event.StudentId}").SendAsync("ReceiveNotification", removedNotification, cancellationToken);

        // Then notify remaining group members
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = "Group Member Removed 👤",
                Content = $"A member has been removed from the group '{@event.GroupName}'.",
                Type = NotificationType.GroupMemberRemoved,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupMemberRemoved", GroupId = @event.GroupId, CourseId = @event.CourseId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} group member removed notifications", @event.GroupMemberIds.Count + 1);
    }

    private async Task HandleGroupLeaderChangedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupLeaderChangedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Group Leader Changed notifies all group members
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = memberId == @event.NewLeaderId ? "You're Now Group Leader! 👑" : "Group Leader Changed 👤",
                Content = memberId == @event.NewLeaderId 
                    ? $"You have been assigned as the new leader of '{@event.GroupName}'." 
                    : $"The leadership of '{@event.GroupName}' has changed.",
                Type = NotificationType.GroupInvitation,
                Priority = memberId == @event.NewLeaderId ? NotificationPriority.High : NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupLeaderChanged", GroupId = @event.GroupId, CourseId = @event.CourseId, PreviousLeaderId = @event.PreviousLeaderId, NewLeaderId = @event.NewLeaderId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} group leader changed notifications", @event.GroupMemberIds.Count);
    }

    private async Task HandleGroupAssignmentAssignedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupAssignmentAssignedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        // Business Rule: Don't send notification if assignment is in Draft status (0)
        if (@event.AssignmentStatus == 0)
        {
            _logger.LogInformation("[ClassroomEventConsumer] Skipping notification for draft assignment {AssignmentId}", @event.AssignmentId);
            return;
        }

        // Business Rule: Group Assignment Assigned notifies all group members
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = "Group Assignment Assigned 📝",
                Content = $"Your group '{@event.GroupName}' has been assigned the assignment '{@event.AssignmentTitle}'.",
                Type = NotificationType.AssignmentCreated,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupAssignmentAssigned", GroupId = @event.GroupId, CourseId = @event.CourseId, AssignmentId = @event.AssignmentId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} group assignment assigned notifications", @event.GroupMemberIds.Count);
    }

    private async Task HandleGroupsAssignedToAssignmentEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupsAssignedToAssignmentEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing GroupsAssignedToAssignmentEvent: {AssignmentTitle} assigned to {GroupCount} groups with {MemberCount} total members", 
            @event.AssignmentTitle, @event.GroupIds.Count, @event.GroupMemberIds.Count);

        // Business Rule: Notify all members of all assigned groups
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = "New Assignment Assigned 📝",
                Content = $"The assignment '{@event.AssignmentTitle}' has been assigned to your group.",
                Type = NotificationType.AssignmentCreated,
                Priority = NotificationPriority.High,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupsAssignedToAssignment", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} notifications for groups assigned to assignment", @event.GroupMemberIds.Count);
    }

    private async Task HandleGroupsUnassignedFromAssignmentEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<GroupsUnassignedFromAssignmentEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing GroupsUnassignedFromAssignmentEvent: {AssignmentTitle} unassigned from {GroupCount} groups with {MemberCount} total members", 
            @event.AssignmentTitle, @event.GroupIds.Count, @event.GroupMemberIds.Count);

        // Business Rule: Notify all members of all unassigned groups
        foreach (var memberId in @event.GroupMemberIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Title = "Assignment Unassigned 🚫",
                Content = $"The assignment '{@event.AssignmentTitle}' has been unassigned from your group.",
                Type = NotificationType.AssignmentCreated,
                Priority = NotificationPriority.Normal,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { Event = "GroupsUnassignedFromAssignment", AssignmentId = @event.AssignmentId, CourseId = @event.CourseId })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} notifications for groups unassigned from assignment", @event.GroupMemberIds.Count);
    }

    private async Task HandleSupportRequestCreatedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SupportRequestCreatedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing SupportRequestCreatedEvent: {Subject} by {RequesterName} in {CourseName}", 
            @event.Subject, @event.RequesterName, @event.CourseName);

        // Map priority enum value to string for display
        var priorityText = @event.Priority switch
        {
            3 => "Urgent",
            2 => "High",
            1 => "Medium",
            0 => "Low",
            _ => "Medium"
        };

        // Map category enum value to string for display
        var categoryText = @event.Category switch
        {
            0 => "Technical",
            1 => "Academic",
            2 => "Administrative",
            3 => "Other",
            _ => "Other"
        };

        // Business Rule: Notify all staff (sent to SupportStaff group via SignalR)
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty, // Group notification, no specific user
            Title = "New Support Request 🆘",
            Content = $"{@event.RequesterName} ({@event.RequesterRole}) needs help with '{@event.Subject}' in {@event.CourseName}. Category: {categoryText}, Priority: {priorityText}",
            Type = NotificationType.System,
            Priority = @event.Priority switch
            {
                3 => NotificationPriority.Urgent, // Urgent
                2 => NotificationPriority.High,   // High
                1 => NotificationPriority.Normal, // Medium
                0 => NotificationPriority.Low,    // Low
                _ => NotificationPriority.Normal
            },
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { 
                Event = "SupportRequestCreated", 
                SupportRequestId = @event.SupportRequestId, 
                CourseId = @event.CourseId,
                RequesterId = @event.RequesterId,
                Category = @event.Category,
                Priority = @event.Priority
            })
        };

        // Save notification to database
        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send to SupportStaff SignalR group
        await hubContext.Clients.Group("SupportStaff").SendAsync("ReceiveNotification", notification, cancellationToken);
        
        _logger.LogInformation("[ClassroomEventConsumer] Notified SupportStaff group about new support request");
    }

    private async Task HandleSupportRequestAcceptedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SupportRequestAcceptedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing SupportRequestAcceptedEvent: {Subject} accepted by {StaffName}", 
            @event.Subject, @event.StaffName);

        // Business Rule: Notify the requester that their support request was accepted and is now in progress
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.RequesterId,
            Title = "Support Request In Progress 🔄",
            Content = $"{@event.StaffName} has accepted your support request '{@event.Subject}' and it is now in progress. You can now chat with them.",
            Type = NotificationType.System,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { 
                Event = "SupportRequestAccepted", 
                SupportRequestId = @event.SupportRequestId, 
                ConversationId = @event.ConversationId,
                CourseId = @event.CourseId,
                RequesterId = @event.RequesterId,
                AssignedStaffId = @event.StaffId,
                AssignedStaffName = @event.StaffName
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await hubContext.Clients.Group($"user_{@event.RequesterId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("[ClassroomEventConsumer] Notified requester {RequesterId} about accepted support request", @event.RequesterId);
    }

    private async Task HandleSupportRequestResolvedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SupportRequestResolvedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing SupportRequestResolvedEvent: {Subject} resolved", @event.Subject);

        // Business Rule: Notify both requester and assigned staff (if different from resolver)
        var notifications = new List<Notification>();

        // Notify requester
        var requesterNotification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.RequesterId,
            Title = "Support Request Resolved ✔️",
            Content = @event.ResolvedBy == @event.RequesterId 
                ? $"You marked the support request '{@event.Subject}' as resolved."
                : $"Your support request '{@event.Subject}' has been marked as resolved by {@event.AssignedStaffName}.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { 
                Event = "SupportRequestResolved", 
                SupportRequestId = @event.SupportRequestId, 
                CourseId = @event.CourseId,
                RequesterId = @event.RequesterId,
                AssignedStaffId = @event.AssignedStaffId,
                AssignedStaffName = @event.AssignedStaffName
            })
        };
        notifications.Add(requesterNotification);

        // Notify assigned staff if exists and is not the resolver
        if (@event.AssignedStaffId.HasValue && @event.ResolvedBy != @event.AssignedStaffId.Value)
        {
            var staffNotification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.AssignedStaffId.Value,
                Title = "Support Request Resolved ✔️",
                Content = $"{@event.RequesterName} marked the support request '{@event.Subject}' as resolved.",
                Type = NotificationType.System,
                Priority = NotificationPriority.Low,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { 
                    Event = "SupportRequestResolved", 
                    SupportRequestId = @event.SupportRequestId, 
                    CourseId = @event.CourseId,
                    RequesterId = @event.RequesterId,
                    AssignedStaffId = @event.AssignedStaffId,
                    AssignedStaffName = @event.AssignedStaffName
                })
            };
            notifications.Add(staffNotification);
        }

        foreach (var notification in notifications)
        {
            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{notification.UserId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[ClassroomEventConsumer] Created {Count} support request resolved notifications", notifications.Count);
    }

    private async Task HandleSupportRequestRejectedEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SupportRequestRejectedEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing SupportRequestRejectedEvent: {Subject} rejected by {StaffName}", @event.Subject, @event.StaffName);

        // Get rejection reason text
        var rejectionReasonText = @event.RejectionReason switch
        {
            1 => "Insufficient Permissions",
            2 => "Require Higher Authorization",
            3 => "Out of Scope",
            4 => "Duplicate Request",
            5 => "Other",
            _ => "Unknown"
        };

        // Notify requester
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = @event.RequesterId,
            Title = "Support Request Rejected ❌",
            Content = $"Your support request '{@event.Subject}' was rejected by {@event.StaffName}. Reason: {rejectionReasonText}{(!string.IsNullOrEmpty(@event.RejectionComments) ? $". {@event.RejectionComments}" : "")}",
            Type = NotificationType.SupportRequestRejected,
            Priority = NotificationPriority.High,
            Source = EventSource.ClassroomService,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "SupportRequestRejected",
                SupportRequestId = @event.SupportRequestId,
                CourseId = @event.CourseId,
                RequesterId = @event.RequesterId,
                RejectedBy = @event.RejectedBy,
                RejectedByStaffName = @event.StaffName,
                RejectionReason = rejectionReasonText,
                RejectionComments = @event.RejectionComments
            })
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send via SignalR
        await hubContext.Clients.Group($"user_{@event.RequesterId}").SendAsync("ReceiveNotification", notification, cancellationToken);

        _logger.LogInformation("[ClassroomEventConsumer] Notified requester {RequesterId} about support request rejection", @event.RequesterId);
    }

    private async Task HandleSupportRequestCancelledEvent(string eventJson, IUnitOfWork unitOfWork, IHubContext hubContext, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<SupportRequestCancelledEventDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        _logger.LogInformation("[ClassroomEventConsumer] Processing SupportRequestCancelledEvent: {Subject} cancelled", @event.Subject);

        // Business Rule: Notify assigned staff only (if exists)
        if (@event.AssignedStaffId.HasValue)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.AssignedStaffId.Value,
                Title = "Support Request Cancelled ❌",
                Content = $"{@event.RequesterName} cancelled the support request '{@event.Subject}'.",
                Type = NotificationType.System,
                Priority = NotificationPriority.Low,
                Source = EventSource.ClassroomService,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { 
                    Event = "SupportRequestCancelled", 
                    SupportRequestId = @event.SupportRequestId, 
                    CourseId = @event.CourseId,
                    RequesterId = @event.RequesterId,
                    AssignedStaffId = @event.AssignedStaffId,
                    AssignedStaffName = @event.AssignedStaffName
                })
            };

            await unitOfWork.Notifications.AddAsync(notification);
            await hubContext.Clients.Group($"user_{@event.AssignedStaffId.Value}").SendAsync("ReceiveNotification", notification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("[ClassroomEventConsumer] Notified staff {StaffId} about cancelled support request", @event.AssignedStaffId.Value);
        }
        else
        {
            _logger.LogInformation("[ClassroomEventConsumer] No staff assigned, skipping cancellation notification");
        }
    }
}

// DTOs for deserializing classroom events
public class StudentEnrolledEventDto
{
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime JoinedAt { get; set; }
    public Guid LecturerId { get; set; }
    public string CourseName { get; set; } = string.Empty;
}

public class AssignmentCreatedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public Guid LecturerId { get; set; }
}

public class AssignmentSubmittedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public Guid SubmitterId { get; set; }
    public List<Guid> EnrolledStudentIds { get; set; } = new(); // All enrolled students to notify
}

public class AssignmentGradedEventDto
{
    public Guid ReportId { get; set; }
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public Guid GradedBy { get; set; }
    public string LecturerName { get; set; } = string.Empty;
    public List<Guid> StudentIds { get; set; } = new();
    public bool IsGroupSubmission { get; set; }
}

// Course Events DTOs
public class CourseCreatedEventDto
{
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public Guid LecturerId { get; set; }
}

public class CourseApprovedEventDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid LecturerId { get; set; }
    public Guid ApprovedBy { get; set; }
}

public class CourseRejectedEventDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid LecturerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid RejectedBy { get; set; }
}

public class CourseStatusChangedEventDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid LecturerId { get; set; }
    public int OldStatus { get; set; }  // Changed from string to int (enum value)
    public int NewStatus { get; set; }  // Changed from string to int (enum value)
    public Guid? ChangedBy { get; set; }
    public string? Comments { get; set; }
}

// Assignment Events DTOs (Additional)
public class AssignmentUpdatedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public Guid LecturerId { get; set; }
}

public class AssignmentDeletedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int GroupsUnassigned { get; set; }
    public Guid LecturerId { get; set; }
}

public class AssignmentClosedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ClosedAt { get; set; }
    public List<Guid> EnrolledStudentIds { get; set; } = new(); // Group member IDs from assigned groups
}

public class AssignmentDueDateExtendedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime OriginalDueDate { get; set; }
    public DateTime ExtendedDueDate { get; set; }
    public List<Guid> EnrolledStudentIds { get; set; } = new(); // Group member IDs from assigned groups
}

public class AssignmentStatusChangedEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OldStatus { get; set; } // AssignmentStatus enum
    public int NewStatus { get; set; } // AssignmentStatus enum
    public bool IsAutomatic { get; set; }
    public Guid LecturerId { get; set; }
}

// Group Events DTOs
public class GroupCreatedEventDto
{
    public Guid GroupId { get; set; }
    public Guid CourseId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public List<Guid> GroupMemberIds { get; set; } = new();
}

public class GroupMemberAddedEventDto
{
    public Guid GroupId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public bool IsLeader { get; set; }
    public Guid AddedBy { get; set; }
    public List<Guid> GroupMemberIds { get; set; } = new();
    public string GroupName { get; set; } = string.Empty;
}

public class GroupMemberRemovedEventDto
{
    public Guid GroupId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public bool WasLeader { get; set; }
    public Guid RemovedBy { get; set; }
    public List<Guid> GroupMemberIds { get; set; } = new();
    public string GroupName { get; set; } = string.Empty;
}

public class GroupLeaderChangedEventDto
{
    public Guid GroupId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? PreviousLeaderId { get; set; }
    public Guid NewLeaderId { get; set; }
    public Guid ChangedBy { get; set; }
    public List<Guid> GroupMemberIds { get; set; } = new();
    public string GroupName { get; set; } = string.Empty;
}

public class GroupAssignmentAssignedEventDto
{
    public Guid GroupId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid AssignedBy { get; set; }
    public List<Guid> GroupMemberIds { get; set; } = new();
    public string GroupName { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public int AssignmentStatus { get; set; }
}

public class GroupsAssignedToAssignmentEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public List<Guid> GroupIds { get; set; } = new();
    public List<Guid> GroupMemberIds { get; set; } = new();
}

public class GroupsUnassignedFromAssignmentEventDto
{
    public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public List<Guid> GroupIds { get; set; } = new();
    public List<Guid> GroupMemberIds { get; set; } = new();
}

public class SupportRequestCreatedEventDto
{
    public Guid SupportRequestId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterRole { get; set; } = string.Empty;
    public int Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class SupportRequestAcceptedEventDto
{
    public Guid SupportRequestId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public Guid ConversationId { get; set; }
    public int Category { get; set; }
    public string Subject { get; set; } = string.Empty;
}

public class SupportRequestResolvedEventDto
{
    public Guid SupportRequestId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public Guid ResolvedBy { get; set; }
    public int Category { get; set; }
    public string Subject { get; set; } = string.Empty;
}

public class SupportRequestRejectedEventDto
{
    public Guid SupportRequestId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid RejectedBy { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int RejectionReason { get; set; }
    public string? RejectionComments { get; set; }
    public string Subject { get; set; } = string.Empty;
}

public class SupportRequestCancelledEventDto
{
    public Guid SupportRequestId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public int Category { get; set; }
    public string Subject { get; set; } = string.Empty;
}



