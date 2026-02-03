using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Events;

/// <summary>
/// Event raised when a user's role is changed
/// </summary>
public class UserRoleChangedEvent : BaseEvent
{
    public Guid UserId { get; }
    public UserRole OldRole { get; }
    public UserRole NewRole { get; }
    public Guid ChangedByUserId { get; }
    public DateTime ChangedAt { get; }
    public string? Reason { get; }

    public UserRoleChangedEvent(
        Guid userId, 
        UserRole oldRole, 
        UserRole newRole, 
        Guid changedByUserId,
        DateTime changedAt,
        string? reason = null)
    {
        UserId = userId;
        OldRole = oldRole;
        NewRole = newRole;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
        Reason = reason;
    }
}
