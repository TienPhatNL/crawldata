using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.EventHandlers;

public class CourseRequestApprovedEventHandler : INotificationHandler<CourseRequestApprovedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<CourseRequestApprovedEventHandler> _logger;

    public CourseRequestApprovedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<CourseRequestApprovedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(CourseRequestApprovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[CourseRequestApprovedEventHandler] Publishing event for approved course request {CourseRequestId}, created course {CreatedCourseId}",
                notification.CourseRequestId,
                notification.CreatedCourseId);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.CourseRequestId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[CourseRequestApprovedEventHandler] Successfully published CourseRequestApprovedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CourseRequestApprovedEventHandler] Error publishing CourseRequestApprovedEvent for request {CourseRequestId}",
                notification.CourseRequestId);
            throw;
        }
    }
}
