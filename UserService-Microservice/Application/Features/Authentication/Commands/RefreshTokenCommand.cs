using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class RefreshTokenCommand : IRequest<ResponseModel>
{
    public string RefreshToken { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}