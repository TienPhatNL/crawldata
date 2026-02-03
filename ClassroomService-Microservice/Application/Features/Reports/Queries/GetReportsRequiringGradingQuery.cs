using MediatR;
using ClassroomService.Application.Features.Reports.DTOs;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsRequiringGradingQuery : IRequest<GetReportsRequiringGradingResponse>
{
    public Guid? CourseId { get; set; }
    public Guid? AssignmentId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetReportsRequiringGradingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ReportDto> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
