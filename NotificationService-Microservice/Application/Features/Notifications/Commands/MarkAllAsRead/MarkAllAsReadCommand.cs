using MediatR;

namespace NotificationService.Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllAsReadCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
}
