using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationTemplate : BaseAuditableEntity
{
    public string TemplateKey { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string TitleTemplate { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public string? EmailSubjectTemplate { get; set; }
    public string? EmailBodyTemplate { get; set; }
    public bool IsActive { get; set; } = true;
}
