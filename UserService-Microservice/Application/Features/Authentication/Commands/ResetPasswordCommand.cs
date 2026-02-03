using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class ResetPasswordCommand : IRequest<ResponseModel>
{
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}