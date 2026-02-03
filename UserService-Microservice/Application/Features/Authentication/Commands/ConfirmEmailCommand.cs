using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Authentication.Commands;

public class ConfirmEmailCommand : IRequest<ResponseModel>
{
    public string Token { get; set; } = null!;
}