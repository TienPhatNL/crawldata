using MediatR;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsByAssignmentQuery : IRequest<GetReportsByAssignmentResponse>
{
    public Guid AssignmentId { get; set; }
    public ReportStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetReportsByAssignmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReportDto> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
