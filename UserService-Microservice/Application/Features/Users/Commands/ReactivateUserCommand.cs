using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class ReactivateUserCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid ReactivatedById { get; set; }
}