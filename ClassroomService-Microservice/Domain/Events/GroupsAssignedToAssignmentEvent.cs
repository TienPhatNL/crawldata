using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class GroupsAssignedToAssignmentEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string AssignmentTitle { get; }
    public List<Guid> GroupIds { get; }
    public List<Guid> GroupMemberIds { get; } // All members from all assigned groups
    public int GroupCount { get; }

    public GroupsAssignedToAssignmentEvent(
        Guid assignmentId, 
        Guid courseId, 
        string assignmentTitle, 
        List<Guid> groupIds,
        List<Guid> groupMemberIds)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        AssignmentTitle = assignmentTitle;
        GroupIds = groupIds;
        GroupMemberIds = groupMemberIds;
        GroupCount = groupIds.Count;
    }
}
