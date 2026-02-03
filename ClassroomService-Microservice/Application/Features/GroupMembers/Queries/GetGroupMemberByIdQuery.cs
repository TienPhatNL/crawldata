using MediatR;

namespace ClassroomService.Application.Features.GroupMembers.Queries;

/// <summary>
/// Query to get a specific group member by ID
/// </summary>
public class GetGroupMemberByIdQuery : IRequest<GetGroupMemberByIdResponse>
{
    public Guid MemberId { get; set; }
}
