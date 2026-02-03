using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Queries;

public class GetUserQuery : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid? RequestingUserId { get; set; } // For authorization
}