using MediatR;
using ClassroomService.Application.Features.Reports.DTOs;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetLateSubmissionsQuery : IRequest<GetLateSubmissionsResponse>
{
    public Guid? CourseId { get; set; }
    public Guid? AssignmentId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetLateSubmissionsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<LateReportDto> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public class LateReportDto : ReportDto
{
    public DateTime Deadline { get; set; }
    public int DaysLate { get; set; }
}
