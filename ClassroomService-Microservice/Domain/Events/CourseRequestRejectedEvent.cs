using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseRequestRejectedEvent : BaseEvent
{
    public Guid CourseRequestId { get; }
    public Guid LecturerId { get; }
    public Guid RejectedBy { get; }
    public string StaffName { get; }
    public string CourseCode { get; }
    public string CourseTitle { get; }
    public string? RejectionComments { get; }

    public CourseRequestRejectedEvent(
        Guid courseRequestId,
        Guid lecturerId,
        Guid rejectedBy,
        string staffName,
        string courseCode,
        string courseTitle,
        string? rejectionComments)
    {
        CourseRequestId = courseRequestId;
        LecturerId = lecturerId;
        RejectedBy = rejectedBy;
        StaffName = staffName;
        CourseCode = courseCode;
        CourseTitle = courseTitle;
        RejectionComments = rejectionComments;
    }
}
