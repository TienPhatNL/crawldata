using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when an assignment is assigned to a group
/// </summary>
public class GroupAssignmentAssignedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public Guid AssignedBy { get; }
    public List<Guid> GroupMemberIds { get; }
    public string GroupName { get; }
    public string AssignmentTitle { get; }
    public int AssignmentStatus { get; }

    public GroupAssignmentAssignedEvent(Guid groupId, Guid assignmentId, Guid courseId, Guid assignedBy, List<Guid> groupMemberIds, string groupName, string assignmentTitle, int assignmentStatus)
    {
        GroupId = groupId;
        AssignmentId = assignmentId;
        CourseId = courseId;
        AssignedBy = assignedBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
        GroupName = groupName;
        AssignmentTitle = assignmentTitle;
        AssignmentStatus = assignmentStatus;
    }
}
