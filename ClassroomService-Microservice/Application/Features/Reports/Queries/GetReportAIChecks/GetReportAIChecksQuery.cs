using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

/// <summary>
/// Query to get all AI checks for a specific report
/// </summary>
public class GetReportAIChecksQuery : IRequest<GetReportAIChecksResponse>
{
    /// <summary>
    /// ID of the report to get AI checks for
    /// </summary>
    public Guid ReportId { get; set; }
}
