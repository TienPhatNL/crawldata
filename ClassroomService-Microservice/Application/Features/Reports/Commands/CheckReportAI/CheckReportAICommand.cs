using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to perform AI content detection check on a report
/// </summary>
public class CheckReportAICommand : IRequest<CheckReportAIResponse>
{
    /// <summary>
    /// ID of the report to check
    /// </summary>
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Optional notes about why this check is being performed
    /// </summary>
    public string? Notes { get; set; }
}
