using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentByIdQuery : IRequest<GetAssignmentByIdResponse>
{
    public Guid AssignmentId { get; set; }
    public Guid? RequestUserId { get; set; }
    public string? RequestUserRole { get; set; }
}
