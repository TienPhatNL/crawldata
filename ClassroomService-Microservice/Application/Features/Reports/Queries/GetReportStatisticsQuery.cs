using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportStatisticsQuery : IRequest<GetReportStatisticsResponse>
{
    public Guid AssignmentId { get; set; }
}

public class GetReportStatisticsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalSubmissions { get; set; }
    public int GradedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal? AverageGrade { get; set; }
    public int LateSubmissions { get; set; }
    public Dictionary<ReportStatus, int> StatusBreakdown { get; set; } = new();
}
