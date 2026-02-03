using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to revert report content to a previous historical version
/// Restores Submission (text content) and FileUrl (file attachment) without changing status
/// </summary>
public class RevertContentReportCommand : IRequest<RevertContentReportResponse>
{
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// The version number to revert to (from report history)
    /// </summary>
    public int Version { get; set; }
    
    public string? Comment { get; set; }
}

/// <summary>
/// Response for reverting report content to a historical version
/// </summary>
public class RevertContentReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
