using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

public class NotificationFailedEvent : BaseEvent
{
    public Guid NotificationId { get; }
    public NotificationChannel Channel { get; }
    public string ErrorMessage { get; }

    public NotificationFailedEvent(Guid notificationId, NotificationChannel channel, string errorMessage)
    {
        NotificationId = notificationId;
        Channel = channel;
        ErrorMessage = errorMessage;
    }
}
