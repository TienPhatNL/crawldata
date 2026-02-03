using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class AssignmentCreatedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public DateTime DueDate { get; }
    public Guid LecturerId { get; }

    public AssignmentCreatedEvent(Guid assignmentId, Guid courseId, string title, DateTime dueDate, Guid lecturerId)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        DueDate = dueDate;
        LecturerId = lecturerId;
    }
}