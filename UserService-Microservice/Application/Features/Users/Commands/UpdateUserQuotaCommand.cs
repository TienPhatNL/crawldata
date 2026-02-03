using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserQuotaCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public int NewQuotaLimit { get; set; }
}