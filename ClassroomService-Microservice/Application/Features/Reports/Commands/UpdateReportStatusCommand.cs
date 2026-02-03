using System.Text.Json.Serialization;
using ClassroomService.Domain.Enums;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to update report status (Student/Leader only)
/// </summary>
public class UpdateReportStatusCommand : IRequest<UpdateReportStatusResponse>
{
    [JsonIgnore]
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Target status: Draft or RequiresRevision
    /// </summary>
    public ReportStatus TargetStatus { get; set; }
    
    /// <summary>
    /// Optional comment explaining the status change
    /// </summary>
    public string? Comment { get; set; }
}

public class UpdateReportStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReportStatus? NewStatus { get; set; }
    
    /// <summary>
    /// ID of the user who made this status change
    /// </summary>
    public Guid? ContributorId { get; set; }
    
    /// <summary>
    /// Full name of the contributor
    /// </summary>
    public string? ContributorName { get; set; }
    
    /// <summary>
    /// Role of the contributor (e.g., Student, Lecturer)
    /// </summary>
    public string? ContributorRole { get; set; }
}
