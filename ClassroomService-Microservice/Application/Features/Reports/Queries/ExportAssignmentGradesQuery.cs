using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

public class ExportAssignmentGradesQuery : IRequest<ExportAssignmentGradesResponse>
{
    public Guid AssignmentId { get; set; }
}

public class ExportAssignmentGradesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public byte[]? FileContent { get; set; }
    public string FileName { get; set; } = string.Empty;
}
