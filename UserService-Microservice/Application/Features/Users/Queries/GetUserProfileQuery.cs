using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Queries;

public class GetUserProfileQuery : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
}