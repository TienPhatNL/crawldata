using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Events;

public class CourseStatusChangedEvent : BaseEvent
{
    public Guid CourseId { get; }
    public CourseStatus OldStatus { get; }
    public CourseStatus NewStatus { get; }
    public Guid? ChangedBy { get; }
    public string? Comments { get; }
    public Guid LecturerId { get; }

    public CourseStatusChangedEvent(
        Guid courseId, 
        CourseStatus oldStatus, 
        CourseStatus newStatus,
        Guid lecturerId,
        Guid? changedBy = null,
        string? comments = null)
    {
        CourseId = courseId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        LecturerId = lecturerId;
        ChangedBy = changedBy;
        Comments = comments;
    }
}
