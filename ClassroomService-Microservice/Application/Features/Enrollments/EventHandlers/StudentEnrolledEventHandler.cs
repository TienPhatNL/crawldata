using MediatR;
using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;

namespace ClassroomService.Application.Features.Enrollments.EventHandlers;

/// <summary>
/// Handles StudentEnrolledEvent - logs student enrollment and publishes to Kafka
/// </summary>
public class StudentEnrolledEventHandler : INotificationHandler<StudentEnrolledEvent>
{
    private readonly ILogger<StudentEnrolledEventHandler> _logger;
    private readonly KafkaEventPublisher _eventPublisher;

    public StudentEnrolledEventHandler(ILogger<StudentEnrolledEventHandler> logger, KafkaEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Student enrolled: {StudentId} enrolled in course {CourseId} ({CourseName}) at {JoinedAt}. Lecturer: {LecturerId}",
            notification.StudentId,
            notification.CourseId,
            notification.CourseName,
            notification.JoinedAt,
            notification.LecturerId);

        // Publish to Kafka for NotificationService
        try
        {
            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.EnrollmentId.ToString(),
                notification,
                cancellationToken);
            
            _logger.LogDebug("StudentEnrolledEvent published to Kafka for enrollment {EnrollmentId}", notification.EnrollmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish StudentEnrolledEvent to Kafka for enrollment {EnrollmentId}", notification.EnrollmentId);
            throw;
        }
    }
}
