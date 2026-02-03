using NotificationService.Domain.Common;

namespace NotificationService.Domain.Events;

public class NotificationReadEvent : BaseEvent
{
    public Guid NotificationId { get; }
    public Guid UserId { get; }

    public NotificationReadEvent(Guid notificationId, Guid userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }
}
