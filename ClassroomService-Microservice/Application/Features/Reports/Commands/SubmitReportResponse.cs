using ClassroomService.Application.Features.Reports.DTOs;

namespace ClassroomService.Application.Features.Reports.Commands;

public class SubmitReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReportId { get; set; }
    public ReportDto? Report { get; set; }
}
