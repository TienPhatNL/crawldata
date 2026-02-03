using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class DeleteAssignmentCommand : IRequest<DeleteAssignmentResponse>
{
    public Guid AssignmentId { get; set; }
}
