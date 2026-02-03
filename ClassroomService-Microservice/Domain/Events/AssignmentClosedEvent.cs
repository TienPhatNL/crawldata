using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class AssignmentClosedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public DateTime ClosedAt { get; }
    public List<Guid> EnrolledStudentIds { get; }

    public AssignmentClosedEvent(Guid assignmentId, Guid courseId, string title, DateTime closedAt, List<Guid> enrolledStudentIds)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        ClosedAt = closedAt;
        EnrolledStudentIds = enrolledStudentIds ?? new List<Guid>();
    }
}
