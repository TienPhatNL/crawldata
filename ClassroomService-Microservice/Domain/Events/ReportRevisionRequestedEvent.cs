using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class ReportRevisionRequestedEvent : BaseEvent
{
    public Guid ReportId { get; }
    public Guid AssignmentId { get; }
    public string AssignmentTitle { get; }
    public Guid CourseId { get; }
    public string? Feedback { get; }
    public Guid RequestedBy { get; }
    public string LecturerName { get; }
    public List<Guid> StudentIds { get; }
    public bool IsGroupSubmission { get; }
    public Guid? GroupId { get; }

    public ReportRevisionRequestedEvent(
        Guid reportId,
        Guid assignmentId,
        string assignmentTitle,
        Guid courseId,
        string? feedback,
        Guid requestedBy,
        string lecturerName,
        List<Guid> studentIds,
        bool isGroupSubmission,
        Guid? groupId)
    {
        ReportId = reportId;
        AssignmentId = assignmentId;
        AssignmentTitle = assignmentTitle;
        CourseId = courseId;
        Feedback = feedback;
        RequestedBy = requestedBy;
        LecturerName = lecturerName;
        StudentIds = studentIds;
        IsGroupSubmission = isGroupSubmission;
        GroupId = groupId;
    }
}
