using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserEmailConfirmedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string Email { get; }

    public UserEmailConfirmedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}