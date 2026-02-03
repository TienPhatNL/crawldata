using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class SupportRequestCancelledEvent : BaseEvent
{
    public Guid SupportRequestId { get; }
    public Guid CourseId { get; }
    public string CourseName { get; }
    public Guid RequesterId { get; }
    public string RequesterName { get; }
    public string Subject { get; }

    public SupportRequestCancelledEvent(
        Guid supportRequestId,
        Guid courseId,
        string courseName,
        Guid requesterId,
        string requesterName,
        string subject)
    {
        SupportRequestId = supportRequestId;
        CourseId = courseId;
        CourseName = courseName;
        RequesterId = requesterId;
        RequesterName = requesterName;
        Subject = subject;
    }
}
