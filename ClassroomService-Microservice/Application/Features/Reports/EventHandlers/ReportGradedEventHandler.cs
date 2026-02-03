using ClassroomService.Domain.Events;
using ClassroomService.Infrastructure.Messaging;
using MediatR;

namespace ClassroomService.Application.Features.Reports.EventHandlers;

public class ReportGradedEventHandler : INotificationHandler<ReportGradedEvent>
{
    private readonly KafkaEventPublisher _eventPublisher;
    private readonly ILogger<ReportGradedEventHandler> _logger;

    public ReportGradedEventHandler(
        KafkaEventPublisher eventPublisher,
        ILogger<ReportGradedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReportGradedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[ReportGradedEventHandler] Publishing event for graded report {ReportId}, Grade: {Grade}",
                notification.ReportId,
                notification.Grade);

            await _eventPublisher.PublishAsync(
                "classroom-events",
                notification.ReportId.ToString(),
                notification,
                cancellationToken);

            _logger.LogInformation(
                "[ReportGradedEventHandler] Successfully published ReportGradedEvent to Kafka");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ReportGradedEventHandler] Error publishing ReportGradedEvent for report {ReportId}",
                notification.ReportId);
            throw;
        }
    }
}
