using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseRejectedEvent : BaseEvent
{
    public Guid CourseId { get; }
    public Guid RejectedBy { get; }
    public string CourseName { get; }
    public string RejectionReason { get; }
    public Guid LecturerId { get; }

    public CourseRejectedEvent(
        Guid courseId, 
        Guid rejectedBy, 
        string courseName,
        Guid lecturerId,
        string rejectionReason)
    {
        CourseId = courseId;
        RejectedBy = rejectedBy;
        CourseName = courseName;
        LecturerId = lecturerId;
        RejectionReason = rejectionReason;
    }
}
