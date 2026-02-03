using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class ChangePasswordCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}