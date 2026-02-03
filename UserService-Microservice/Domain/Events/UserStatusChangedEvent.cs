using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Events;

public class UserStatusChangedEvent : BaseEvent
{
    public Guid UserId { get; }
    public UserStatus OldStatus { get; }
    public UserStatus NewStatus { get; }
    public string? Reason { get; }
    public Guid? ChangedBy { get; }

    public UserStatusChangedEvent(Guid userId, UserStatus oldStatus, UserStatus newStatus, string? reason = null, Guid? changedBy = null)
    {
        UserId = userId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
        ChangedBy = changedBy;
    }
}