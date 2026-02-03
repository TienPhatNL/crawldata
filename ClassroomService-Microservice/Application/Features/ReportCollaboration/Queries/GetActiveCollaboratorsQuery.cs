using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetActiveCollaboratorsQuery : IRequest<GetActiveCollaboratorsResponse>
{
    public Guid ReportId { get; set; }
}

public class GetActiveCollaboratorsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CollaboratorPresenceDto> Collaborators { get; set; } = new();
}
