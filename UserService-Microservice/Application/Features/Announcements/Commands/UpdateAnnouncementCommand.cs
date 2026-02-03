using System.Text.Json.Serialization;
using MediatR;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Announcements.Commands;

public class UpdateAnnouncementCommand : IRequest<ResponseModel>
{
    [JsonIgnore]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.All;
    public DateTime? PublishedAt { get; set; }
}
