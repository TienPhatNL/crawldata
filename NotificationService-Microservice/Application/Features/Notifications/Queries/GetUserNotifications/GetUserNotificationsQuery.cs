using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Notifications.Queries.GetUserNotifications;

public class GetUserNotificationsQuery : IRequest<IEnumerable<Notification>>
{
    public Guid UserId { get; set; }
    public bool? IsRead { get; set; }
    public int Take { get; set; } = 50;
    public bool IsStaff { get; set; } = false;
}
