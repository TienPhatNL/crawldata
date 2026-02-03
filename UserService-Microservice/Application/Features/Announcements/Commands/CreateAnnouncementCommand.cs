using MediatR;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Announcements.Commands;

public class CreateAnnouncementCommand : IRequest<ResponseModel>
{
    public Guid CreatedBy { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.All;
    public DateTime? PublishedAt { get; set; }

    public CreateAnnouncementCommand()
    {
    }

    public CreateAnnouncementCommand(Guid createdBy)
    {
        CreatedBy = createdBy;
    }

    public void SetCreator(Guid creatorId)
    {
        CreatedBy = creatorId;
    }
}
