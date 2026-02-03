using MediatR;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsByCourseQuery : IRequest<GetReportsByCourseResponse>
{
    public Guid CourseId { get; set; }
    public ReportStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetReportsByCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReportDto> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
