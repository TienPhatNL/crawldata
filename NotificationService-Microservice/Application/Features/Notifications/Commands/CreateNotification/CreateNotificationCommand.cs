using MediatR;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public EventSource Source { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<NotificationChannel> Channels { get; set; } = new();
}
