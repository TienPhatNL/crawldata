using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserSuspendedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string UserEmail { get; }
    public string SuspensionReason { get; }
    public Guid SuspendedById { get; }

    public UserSuspendedEvent(Guid userId, string userEmail, string suspensionReason, Guid suspendedById)
    {
        UserId = userId;
        UserEmail = userEmail;
        SuspensionReason = suspensionReason;
        SuspendedById = suspendedById;
    }
}