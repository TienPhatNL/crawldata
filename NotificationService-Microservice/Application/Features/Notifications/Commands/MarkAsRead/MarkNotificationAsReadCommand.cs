using MediatR;

namespace NotificationService.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkNotificationAsReadCommand : IRequest<bool>
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
}
