using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when a group leader is changed
/// </summary>
public class GroupLeaderChangedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid CourseId { get; }
    public Guid? PreviousLeaderId { get; }
    public Guid NewLeaderId { get; }
    public Guid ChangedBy { get; }
    public List<Guid> GroupMemberIds { get; }
    public string GroupName { get; }

    public GroupLeaderChangedEvent(Guid groupId, Guid courseId, Guid? previousLeaderId, Guid newLeaderId, Guid changedBy, List<Guid> groupMemberIds, string groupName)
    {
        GroupId = groupId;
        CourseId = courseId;
        PreviousLeaderId = previousLeaderId;
        NewLeaderId = newLeaderId;
        ChangedBy = changedBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
        GroupName = groupName;
    }
}
