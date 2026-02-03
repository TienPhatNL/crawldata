using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class ResetUserQuotaCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
}