using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetPendingChangesCountQuery : IRequest<GetPendingChangesCountResponse>
{
    public Guid ReportId { get; set; }
}

public class GetPendingChangesCountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}
