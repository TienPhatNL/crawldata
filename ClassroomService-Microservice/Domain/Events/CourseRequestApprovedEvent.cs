using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class CourseRequestApprovedEvent : BaseEvent
{
    public Guid CourseRequestId { get; }
    public Guid CreatedCourseId { get; }
    public string CourseName { get; }
    public string CourseCode { get; }
    public Guid LecturerId { get; }
    public Guid ApprovedBy { get; }
    public string StaffName { get; }
    public string? ApprovalComments { get; }

    public CourseRequestApprovedEvent(
        Guid courseRequestId,
        Guid createdCourseId,
        string courseName,
        string courseCode,
        Guid lecturerId,
        Guid approvedBy,
        string staffName,
        string? approvalComments)
    {
        CourseRequestId = courseRequestId;
        CreatedCourseId = createdCourseId;
        CourseName = courseName;
        CourseCode = courseCode;
        LecturerId = lecturerId;
        ApprovedBy = approvedBy;
        StaffName = staffName;
        ApprovalComments = approvalComments;
    }
}
