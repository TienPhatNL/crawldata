using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Commands;

public class UnassignGroupLeaderCommand : IRequest<UnassignGroupLeaderResponse>
{
    public Guid GroupId { get; set; }
}
