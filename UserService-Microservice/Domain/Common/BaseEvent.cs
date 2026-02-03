using MediatR;

namespace UserService.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime OccurredOn { get; protected set; }

    protected BaseEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }
}