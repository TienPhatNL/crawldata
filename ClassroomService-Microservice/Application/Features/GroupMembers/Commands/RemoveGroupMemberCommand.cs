using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class RemoveGroupMemberCommand : IRequest<RemoveGroupMemberResponse>
{
    public Guid GroupId { get; set; }
    public Guid StudentId { get; set; }
}
