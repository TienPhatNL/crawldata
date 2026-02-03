using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementByIdQuery : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
}
