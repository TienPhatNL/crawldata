using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Events;

public class AssignmentStatusChangedEvent : BaseEvent
{
    public Guid AssignmentId { get; }
    public Guid CourseId { get; }
    public string Title { get; }
    public AssignmentStatus OldStatus { get; }
    public AssignmentStatus NewStatus { get; }
    public bool IsAutomatic { get; }
    public Guid LecturerId { get; }

    public AssignmentStatusChangedEvent(
        Guid assignmentId, 
        Guid courseId, 
        string title, 
        AssignmentStatus oldStatus, 
        AssignmentStatus newStatus,
        Guid lecturerId,
        bool isAutomatic = false)
    {
        AssignmentId = assignmentId;
        CourseId = courseId;
        Title = title;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        LecturerId = lecturerId;
        IsAutomatic = isAutomatic;
    }
}
