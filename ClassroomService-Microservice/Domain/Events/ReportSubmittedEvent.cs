using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class ReportSubmittedEvent : BaseEvent
{
    public Guid ReportId { get; }
    public Guid AssignmentId { get; }
    public string AssignmentTitle { get; }
    public Guid CourseId { get; }
    public string CourseName { get; }
    public Guid SubmittedBy { get; }
    public string SubmitterName { get; }
    public bool IsGroupSubmission { get; }
    public Guid? GroupId { get; }
    public string? GroupName { get; }
    public Guid LecturerId { get; }

    public ReportSubmittedEvent(
        Guid reportId,
        Guid assignmentId,
        string assignmentTitle,
        Guid courseId,
        string courseName,
        Guid submittedBy,
        string submitterName,
        bool isGroupSubmission,
        Guid? groupId,
        string? groupName,
        Guid lecturerId)
    {
        ReportId = reportId;
        AssignmentId = assignmentId;
        AssignmentTitle = assignmentTitle;
        CourseId = courseId;
        CourseName = courseName;
        SubmittedBy = submittedBy;
        SubmitterName = submitterName;
        IsGroupSubmission = isGroupSubmission;
        GroupId = groupId;
        GroupName = groupName;
        LecturerId = lecturerId;
    }
}
