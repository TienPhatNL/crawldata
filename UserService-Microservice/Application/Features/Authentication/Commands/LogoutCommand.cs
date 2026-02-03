using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class LogoutCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public string? AccessToken { get; set; }
    public bool LogoutAllDevices { get; set; } = false;
}