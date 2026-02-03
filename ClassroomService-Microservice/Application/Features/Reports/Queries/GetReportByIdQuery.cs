using ClassroomService.Application.Features.Reports.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportByIdQuery : IRequest<GetReportByIdResponse>
{
    public Guid ReportId { get; set; }
}

public class GetReportByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReportDetailDto? Report { get; set; }
}
