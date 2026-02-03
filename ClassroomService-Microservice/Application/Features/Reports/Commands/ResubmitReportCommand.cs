using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class ResubmitReportCommand : IRequest<ResubmitReportResponse>
{
    public Guid ReportId { get; set; }
}

public class ResubmitReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the user who resubmitted the report
    /// </summary>
    public Guid? ContributorId { get; set; }
    
    /// <summary>
    /// Full name of the user who resubmitted the report
    /// </summary>
    public string? ContributorName { get; set; }
    
    /// <summary>
    /// Role of the user who resubmitted the report
    /// </summary>
    public string? ContributorRole { get; set; }
}
