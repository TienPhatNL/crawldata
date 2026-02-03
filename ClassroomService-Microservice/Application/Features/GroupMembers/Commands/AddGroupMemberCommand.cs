using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

/// <summary>
/// Command to add a member to a group
/// </summary>
public class AddGroupMemberCommand : IRequest<AddGroupMemberResponse>
{
    public Guid GroupId { get; set; }
    public Guid StudentId { get; set; }
    public bool IsLeader { get; set; } = false;
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
    public string? Notes { get; set; }
}
