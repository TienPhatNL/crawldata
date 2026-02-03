using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

/// <summary>
/// Command to add multiple students to a group at once
/// </summary>
public class AddMultipleGroupMembersCommand : IRequest<AddMultipleGroupMembersResponse>
{
    public Guid GroupId { get; set; }
    public List<Guid> StudentIds { get; set; } = new();
}
