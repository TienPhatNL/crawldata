using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class ApproveUserCommand : IRequest<ResponseModel>
{
    public Guid UserId { get; set; }
    public Guid ApprovedBy { get; set; } // Staff member approving
    public string? ApprovalNotes { get; set; }
    public bool Approved { get; set; } = true;
    public string? RejectionReason { get; set; }
}