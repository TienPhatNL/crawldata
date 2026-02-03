using MediatR;

namespace NotificationService.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; protected set; }

    protected BaseEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }
}
