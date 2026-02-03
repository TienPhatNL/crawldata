using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class LoginUserCommand : IRequest<ResponseModel>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; } = false;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}