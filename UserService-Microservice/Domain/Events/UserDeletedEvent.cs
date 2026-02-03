using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserDeletedEvent : BaseEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public bool IsPermanentDelete { get; }
    public Guid? DeletedBy { get; }

    public UserDeletedEvent(Guid userId, string email, bool isPermanentDelete, Guid? deletedBy = null)
    {
        UserId = userId;
        Email = email;
        IsPermanentDelete = isPermanentDelete;
        DeletedBy = deletedBy;
    }
}