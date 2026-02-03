using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseRequestCreatedEvent : BaseEvent
{
    public Guid CourseRequestId { get; }
    public Guid LecturerId { get; }
    public string LecturerName { get; }
    public string CourseCode { get; }
    public string CourseTitle { get; }
    public string Term { get; }
    public int Year { get; }
    public string? RequestReason { get; }

    public CourseRequestCreatedEvent(
        Guid courseRequestId,
        Guid lecturerId,
        string lecturerName,
        string courseCode,
        string courseTitle,
        string term,
        int year,
        string? requestReason)
    {
        CourseRequestId = courseRequestId;
        LecturerId = lecturerId;
        LecturerName = lecturerName;
        CourseCode = courseCode;
        CourseTitle = courseTitle;
        Term = term;
        Year = year;
        RequestReason = requestReason;
    }
}
