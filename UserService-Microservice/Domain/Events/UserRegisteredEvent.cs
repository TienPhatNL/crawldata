using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Events;

public class UserRegisteredEvent : BaseEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public UserRole Role { get; }
    public bool RequiresApproval { get; }

    public UserRegisteredEvent(Guid userId, string email, UserRole role, bool requiresApproval)
    {
        UserId = userId;
        Email = email;
        Role = role;
        RequiresApproval = requiresApproval;
    }
}