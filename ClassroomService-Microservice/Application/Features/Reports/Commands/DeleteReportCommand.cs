using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class DeleteReportCommand : IRequest<DeleteReportResponse>
{
    public Guid ReportId { get; set; }
}

public class DeleteReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
