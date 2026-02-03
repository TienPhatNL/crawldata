using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.Reports.EventHandlers;

public class ReportRevisionRequestedEventHandler : INotificationHandler<ReportRevisionRequestedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<ReportRevisionRequestedEventHandler> _logger;

    public ReportRevisionRequestedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<ReportRevisionRequestedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReportRevisionRequestedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[ReportRevisionRequestedEventHandler] Publishing event for report {ReportId} requiring revision",
                notification.ReportId);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.ReportId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[ReportRevisionRequestedEventHandler] Successfully published ReportRevisionRequestedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ReportRevisionRequestedEventHandler] Error publishing ReportRevisionRequestedEvent for report {ReportId}",
                notification.ReportId);
            throw;
        }
    }
}
