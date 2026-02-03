using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when a new group is created
/// </summary>
public class GroupCreatedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid CourseId { get; }
    public string GroupName { get; }
    public Guid CreatedBy { get; }
    public List<Guid> GroupMemberIds { get; }

    public GroupCreatedEvent(Guid groupId, Guid courseId, string groupName, Guid createdBy, List<Guid> groupMemberIds)
    {
        GroupId = groupId;
        CourseId = courseId;
        GroupName = groupName;
        CreatedBy = createdBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
    }
}
