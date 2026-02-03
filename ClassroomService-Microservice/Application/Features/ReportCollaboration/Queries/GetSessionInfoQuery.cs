using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetSessionInfoQuery : IRequest<GetSessionInfoResponse>
{
    public Guid ReportId { get; set; }
}

public class GetSessionInfoResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReportCollaborationSessionDto? Session { get; set; }
}
