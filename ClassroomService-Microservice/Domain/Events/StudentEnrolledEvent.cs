using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class StudentEnrolledEvent : BaseEvent
{
    public Guid EnrollmentId { get; }
    public Guid CourseId { get; }
    public Guid StudentId { get; }
    public DateTime JoinedAt { get; }
    public Guid LecturerId { get; }
    public string CourseName { get; }

    public StudentEnrolledEvent(Guid enrollmentId, Guid courseId, Guid studentId, DateTime joinedAt, Guid lecturerId, string courseName)
    {
        EnrollmentId = enrollmentId;
        CourseId = courseId;
        StudentId = studentId;
        JoinedAt = joinedAt;
        LecturerId = lecturerId;
        CourseName = courseName;
    }
}