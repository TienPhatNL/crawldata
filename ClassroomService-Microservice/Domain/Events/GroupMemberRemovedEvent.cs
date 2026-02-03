using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when a member is removed from a group
/// </summary>
public class GroupMemberRemovedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid CourseId { get; }
    public bool WasLeader { get; }
    public Guid RemovedBy { get; }
    public List<Guid> GroupMemberIds { get; }
    public string GroupName { get; }

    public GroupMemberRemovedEvent(Guid groupId, Guid enrollmentId, Guid studentId, Guid courseId, bool wasLeader, Guid removedBy, List<Guid> groupMemberIds, string groupName)
    {
        GroupId = groupId;
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        CourseId = courseId;
        WasLeader = wasLeader;
        RemovedBy = removedBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
        GroupName = groupName;
    }
}
