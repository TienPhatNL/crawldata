using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.Reports.EventHandlers;

public class ReportSubmittedEventHandler : INotificationHandler<ReportSubmittedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<ReportSubmittedEventHandler> _logger;

    public ReportSubmittedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<ReportSubmittedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReportSubmittedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[ReportSubmittedEventHandler] Publishing event for report {ReportId} submitted by {SubmitterName}",
                notification.ReportId,
                notification.SubmitterName);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.ReportId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[ReportSubmittedEventHandler] Successfully published ReportSubmittedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ReportSubmittedEventHandler] Error publishing ReportSubmittedEvent for report {ReportId}",
                notification.ReportId);
            throw;
        }
    }
}
