using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

/// <summary>
/// Event raised when a group leader reverts report content to a previous version
/// Notifies all group members about the content revert
/// </summary>
public class ReportContentRevertedEvent : BaseEvent
{
    public Guid ReportId { get; }
    public Guid CourseId { get; }
    public Guid AssignmentId { get; }
    public string AssignmentTitle { get; }
    public Guid GroupId { get; }
    public string GroupName { get; }
    public Guid RevertedBy { get; }
    public string RevertedByName { get; }
    public int RevertedToVersion { get; }
    public List<Guid> GroupMemberIds { get; }
    public string? Comment { get; }

    public ReportContentRevertedEvent(
        Guid reportId,
        Guid courseId,
        Guid assignmentId,
        string assignmentTitle,
        Guid groupId,
        string groupName,
        Guid revertedBy,
        string revertedByName,
        int revertedToVersion,
        List<Guid> groupMemberIds,
        string? comment = null)
    {
        ReportId = reportId;
        CourseId = courseId;
        AssignmentId = assignmentId;
        AssignmentTitle = assignmentTitle;
        GroupId = groupId;
        GroupName = groupName;
        RevertedBy = revertedBy;
        RevertedByName = revertedByName;
        RevertedToVersion = revertedToVersion;
        GroupMemberIds = groupMemberIds;
        Comment = comment;
    }
}
