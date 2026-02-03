using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.Reports.EventHandlers;

public class ReportResubmittedEventHandler : INotificationHandler<ReportResubmittedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<ReportResubmittedEventHandler> _logger;

    public ReportResubmittedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<ReportResubmittedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReportResubmittedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[ReportResubmittedEventHandler] Publishing event for resubmitted report {ReportId}, Version: {Version}",
                notification.ReportId,
                notification.Version);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.ReportId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[ReportResubmittedEventHandler] Successfully published ReportResubmittedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ReportResubmittedEventHandler] Error publishing ReportResubmittedEvent for report {ReportId}",
                notification.ReportId);
            throw;
        }
    }
}
