using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentGroupsQuery : IRequest<GetAssignmentGroupsResponse>
{
    public Guid AssignmentId { get; set; }
}
