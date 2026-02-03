using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class GoogleLoginCommand : IRequest<ResponseModel>
{
    public string GoogleIdToken { get; set; } = null!;
    public bool RememberMe { get; set; } = false;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
