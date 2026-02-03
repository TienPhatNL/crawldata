using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class ReportResubmittedEvent : BaseEvent
{
    public Guid ReportId { get; }
    public Guid AssignmentId { get; }
    public string AssignmentTitle { get; }
    public Guid CourseId { get; }
    public Guid SubmittedBy { get; }
    public string SubmitterName { get; }
    public int Version { get; }
    public Guid? GroupId { get; }
    public Guid LecturerId { get; }

    public ReportResubmittedEvent(
        Guid reportId,
        Guid assignmentId,
        string assignmentTitle,
        Guid courseId,
        Guid submittedBy,
        string submitterName,
        int version,
        Guid? groupId,
        Guid lecturerId)
    {
        ReportId = reportId;
        AssignmentId = assignmentId;
        AssignmentTitle = assignmentTitle;
        CourseId = courseId;
        SubmittedBy = submittedBy;
        SubmitterName = submitterName;
        Version = version;
        GroupId = groupId;
        LecturerId = lecturerId;
    }
}
