using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class HasUnsavedChangesQuery : IRequest<HasUnsavedChangesResponse>
{
    public Guid ReportId { get; set; }
}

public class HasUnsavedChangesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool HasUnsavedChanges { get; set; }
}
