using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.Reports.EventHandlers;

/// <summary>
/// Publishes ReportContentRevertedEvent to Kafka for NotificationService
/// Notifies all group members when leader reverts report content
/// </summary>
public class ReportContentRevertedEventHandler : INotificationHandler<ReportContentRevertedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<ReportContentRevertedEventHandler> _logger;

    public ReportContentRevertedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<ReportContentRevertedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReportContentRevertedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[ReportContentRevertedEventHandler] Publishing event for report {ReportId}. Group members: {MemberCount}",
                notification.ReportId,
                notification.GroupMemberIds.Count);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.ReportId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[ReportContentRevertedEventHandler] Successfully published ReportContentRevertedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[ReportContentRevertedEventHandler] Failed to publish ReportContentRevertedEvent for Report {ReportId}", 
                notification.ReportId);
            throw;
        }
    }
}
