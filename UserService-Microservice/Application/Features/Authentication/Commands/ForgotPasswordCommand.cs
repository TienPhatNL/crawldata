using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class ForgotPasswordCommand : IRequest<ResponseModel>
{
    public string Email { get; set; } = null!;
}