using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class SuspendUserCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid SuspendedById { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime? SuspendUntil { get; set; }
}