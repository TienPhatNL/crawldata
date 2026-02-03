using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseCreatedEvent : BaseEvent
{
    public Guid CourseId { get; }
    public string CourseCode { get; }
    public string CourseName { get; }
    public Guid LecturerId { get; }

    public CourseCreatedEvent(Guid courseId, string courseCode, string courseName, Guid lecturerId)
    {
        CourseId = courseId;
        CourseCode = courseCode;
        CourseName = courseName;
        LecturerId = lecturerId;
    }
}