using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class AssignmentUpdatedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public DateTime UpdatedAt { get; }
    public Guid LecturerId { get; }

    public AssignmentUpdatedEvent(Guid assignmentId, Guid courseId, string title, DateTime updatedAt, Guid lecturerId)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        UpdatedAt = updatedAt;
        LecturerId = lecturerId;
    }
}
