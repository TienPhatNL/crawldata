using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class AssignmentDueDateExtendedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public DateTime OriginalDueDate { get; }
    public DateTime ExtendedDueDate { get; }
    public List<Guid> EnrolledStudentIds { get; }

    public AssignmentDueDateExtendedEvent(
        Guid assignmentId, 
        Guid courseId, 
        string title, 
        DateTime originalDueDate, 
        DateTime extendedDueDate,
        List<Guid> enrolledStudentIds)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        OriginalDueDate = originalDueDate;
        ExtendedDueDate = extendedDueDate;
        EnrolledStudentIds = enrolledStudentIds ?? new List<Guid>();
    }
}
