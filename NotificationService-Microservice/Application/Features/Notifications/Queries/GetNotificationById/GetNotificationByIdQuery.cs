using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Notifications.Queries.GetNotificationById;

public class GetNotificationByIdQuery : IRequest<Notification?>
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
}
