using NotificationService.Domain.Common;

namespace NotificationService.Domain.Events;

public class NotificationCreatedEvent : BaseEvent
{
    public Guid NotificationId { get; }
    public Guid UserId { get; }
    public string Title { get; }

    public NotificationCreatedEvent(Guid notificationId, Guid userId, string title)
    {
        NotificationId = notificationId;
        UserId = userId;
        Title = title;
    }
}
