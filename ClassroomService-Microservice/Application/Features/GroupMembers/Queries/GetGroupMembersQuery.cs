using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

public class GetGroupMembersQuery : IRequest<GetGroupMembersResponse>
{
    public Guid GroupId { get; set; }
}
