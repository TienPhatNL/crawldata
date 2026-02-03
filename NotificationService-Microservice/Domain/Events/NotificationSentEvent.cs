using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

public class NotificationSentEvent : BaseEvent
{
    public Guid NotificationId { get; }
    public Guid UserId { get; }
    public NotificationChannel Channel { get; }

    public NotificationSentEvent(Guid notificationId, Guid userId, NotificationChannel channel)
    {
        NotificationId = notificationId;
        UserId = userId;
        Channel = channel;
    }
}
