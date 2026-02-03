using System.Collections.Generic;
using MediatR;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementsQuery : IRequest<ResponseModel>
{
    public List<AnnouncementAudience> Audiences { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
}
