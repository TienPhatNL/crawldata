using MediatR;

namespace ClassroomService.Domain.Common;

public abstract class BaseEvent : INotification
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}