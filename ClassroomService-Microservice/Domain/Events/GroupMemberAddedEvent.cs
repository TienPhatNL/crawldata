using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when a member is added to a group
/// </summary>
public class GroupMemberAddedEvent : BaseEvent
{
    public Guid GroupId { get; }
    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid CourseId { get; }
    public bool IsLeader { get; }
    public Guid AddedBy { get; }
    public List<Guid> GroupMemberIds { get; }
    public string GroupName { get; }

    public GroupMemberAddedEvent(Guid groupId, Guid enrollmentId, Guid studentId, Guid courseId, bool isLeader, Guid addedBy, List<Guid> groupMemberIds, string groupName)
    {
        GroupId = groupId;
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        CourseId = courseId;
        IsLeader = isLeader;
        AddedBy = addedBy;
        GroupMemberIds = groupMemberIds ?? new List<Guid>();
        GroupName = groupName;
    }
}
