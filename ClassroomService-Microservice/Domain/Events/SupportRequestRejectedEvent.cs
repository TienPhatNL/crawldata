using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Events;

public class SupportRequestRejectedEvent : BaseEvent
{
    public Guid SupportRequestId { get; }
    public Guid CourseId { get; }
    public string CourseName { get; }
    public Guid RequesterId { get; }
    public string RequesterName { get; }
    public Guid RejectedBy { get; }
    public string StaffName { get; }
    public SupportRequestRejectionReason RejectionReason { get; }
    public string? RejectionComments { get; }
    public string Subject { get; }

    public SupportRequestRejectedEvent(
        Guid supportRequestId,
        Guid courseId,
        string courseName,
        Guid requesterId,
        string requesterName,
        Guid rejectedBy,
        string staffName,
        SupportRequestRejectionReason rejectionReason,
        string? rejectionComments,
        string subject)
    {
        SupportRequestId = supportRequestId;
        CourseId = courseId;
        CourseName = courseName;
        RequesterId = requesterId;
        RequesterName = requesterName;
        RejectedBy = rejectedBy;
        StaffName = staffName;
        RejectionReason = rejectionReason;
        RejectionComments = rejectionComments;
        Subject = subject;
    }
}
