using MediatR;

namespace NotificationService.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQuery : IRequest<int>
{
    public Guid UserId { get; set; }
    public bool IsStaff { get; set; } = false;
}
