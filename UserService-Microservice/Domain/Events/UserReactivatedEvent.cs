using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserReactivatedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string UserEmail { get; }
    public Guid ReactivatedById { get; }

    public UserReactivatedEvent(Guid userId, string userEmail, Guid reactivatedById)
    {
        UserId = userId;
        UserEmail = userEmail;
        ReactivatedById = reactivatedById;
    }
}