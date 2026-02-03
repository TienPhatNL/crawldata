using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Events;

public class SupportRequestResolvedEvent : BaseEvent
{
    public Guid SupportRequestId { get; }
    public Guid CourseId { get; }
    public string CourseName { get; }
    public Guid RequesterId { get; }
    public string RequesterName { get; }
    public Guid? StaffId { get; }
    public string? StaffName { get; }
    public Guid ResolvedByUserId { get; }
    public string Subject { get; }

    public SupportRequestResolvedEvent(
        Guid supportRequestId,
        Guid courseId,
        string courseName,
        Guid requesterId,
        string requesterName,
        Guid? staffId,
        string? staffName,
        Guid resolvedByUserId,
        string subject)
    {
        SupportRequestId = supportRequestId;
        CourseId = courseId;
        CourseName = courseName;
        RequesterId = requesterId;
        RequesterName = requesterName;
        StaffId = staffId;
        StaffName = staffName;
        ResolvedByUserId = resolvedByUserId;
        Subject = subject;
    }
}
