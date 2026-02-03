using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class BulkUpdateReportStatusCommand : IRequest<BulkUpdateReportStatusResponse>
{
    public List<Guid> ReportIds { get; set; } = new();
}

public class BulkUpdateReportStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UpdatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
