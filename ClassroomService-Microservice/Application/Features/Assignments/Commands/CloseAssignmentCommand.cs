using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class CloseAssignmentCommand : IRequest<CloseAssignmentResponse>
{
    public Guid AssignmentId { get; set; }
}
