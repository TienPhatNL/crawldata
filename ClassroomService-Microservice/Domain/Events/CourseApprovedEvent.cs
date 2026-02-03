using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseApprovedEvent : BaseEvent
{
    public Guid CourseId { get; }
    public Guid ApprovedBy { get; }
    public string CourseName { get; }
    public string? Comments { get; }
    public Guid LecturerId { get; }

    public CourseApprovedEvent(
        Guid courseId, 
        Guid approvedBy, 
        string courseName,
        Guid lecturerId,
        string? comments = null)
    {
        CourseId = courseId;
        ApprovedBy = approvedBy;
        CourseName = courseName;
        LecturerId = lecturerId;
        Comments = comments;
    }
}
