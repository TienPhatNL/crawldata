using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when an assignment is unassigned from a group
/// </summary>
public class GroupAssignmentUnassignedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public Guid UnassignedBy { get; }
    public List<Guid> GroupMemberIds { get; }
    public string GroupName { get; }
    public string AssignmentTitle { get; }

    public GroupAssignmentUnassignedEvent(Guid groupId, Guid assignmentId, Guid courseId, Guid unassignedBy, List<Guid> groupMemberIds, string groupName, string assignmentTitle)
    {
        GroupId = groupId;
        AssignmentId = assignmentId;
        CourseId = courseId;
        UnassignedBy = unassignedBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
        GroupName = groupName;
        AssignmentTitle = assignmentTitle;
    }
}
