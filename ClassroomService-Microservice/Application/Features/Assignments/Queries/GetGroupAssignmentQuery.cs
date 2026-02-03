using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetGroupAssignmentQuery : IRequest<GetGroupAssignmentResponse>
{
    public Guid GroupId { get; set; }
    public Guid? RequestUserId { get; set; }
    public string? RequestUserRole { get; set; }
}
