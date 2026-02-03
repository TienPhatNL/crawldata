using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Events;

public class SupportRequestCreatedEvent : BaseEvent
{
    public Guid SupportRequestId { get; }
    public Guid CourseId { get; }
    public string CourseName { get; }
    public Guid RequesterId { get; }
    public string RequesterName { get; }
    public string RequesterRole { get; }
    public SupportPriority Priority { get; }
    public SupportRequestCategory Category { get; }
    public string Subject { get; }

    public SupportRequestCreatedEvent(
        Guid supportRequestId,
        Guid courseId,
        string courseName,
        Guid requesterId,
        string requesterName,
        string requesterRole,
        SupportPriority priority,
        SupportRequestCategory category,
        string subject)
    {
        SupportRequestId = supportRequestId;
        CourseId = courseId;
        CourseName = courseName;
        RequesterId = requesterId;
        RequesterName = requesterName;
        RequesterRole = requesterRole;
        Priority = priority;
        Category = category;
        Subject = subject;
    }
}
