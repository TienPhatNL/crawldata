using UserService.Domain.Common;

namespace UserService.Domain.Events;

public class UserProfileUpdatedEvent : BaseEvent
{
    public Guid UserId { get; }
    public Dictionary<string, object> ChangedFields { get; }

    public UserProfileUpdatedEvent(Guid userId, Dictionary<string, object> changedFields)
    {
        UserId = userId;
        ChangedFields = changedFields;
    }
}