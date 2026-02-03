using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class AssignmentDeletedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public int GroupsUnassigned { get; }
    public Guid LecturerId { get; }

    public AssignmentDeletedEvent(Guid assignmentId, Guid courseId, string title, int groupsUnassigned, Guid lecturerId)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        GroupsUnassigned = groupsUnassigned;
        LecturerId = lecturerId;
    }
}
