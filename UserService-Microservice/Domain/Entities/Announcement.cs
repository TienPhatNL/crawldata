using UserService.Domain.Common;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class Announcement : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.All;
    public Guid CreatedBy { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
