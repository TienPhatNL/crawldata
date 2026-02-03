using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class ReportGradedEvent : BaseEvent
{
    public Guid ReportId { get; }
    public Guid AssignmentId { get; }
    public string AssignmentTitle { get; }
    public Guid CourseId { get; }
    public decimal Grade { get; }
    public int? MaxPoints { get; }
    public string? Feedback { get; }
    public Guid GradedBy { get; }
    public string LecturerName { get; }
    public List<Guid> StudentIds { get; }
    public bool IsGroupSubmission { get; }
    public Guid? GroupId { get; }

    public ReportGradedEvent(
        Guid reportId,
        Guid assignmentId,
        string assignmentTitle,
        Guid courseId,
        decimal grade,
        int? maxPoints,
        string? feedback,
        Guid gradedBy,
        string lecturerName,
        List<Guid> studentIds,
        bool isGroupSubmission,
        Guid? groupId)
    {
        ReportId = reportId;
        AssignmentId = assignmentId;
        AssignmentTitle = assignmentTitle;
        CourseId = courseId;
        Grade = grade;
        MaxPoints = maxPoints;
        Feedback = feedback;
        GradedBy = gradedBy;
        LecturerName = lecturerName;
        StudentIds = studentIds;
        IsGroupSubmission = isGroupSubmission;
        GroupId = groupId;
    }
}
