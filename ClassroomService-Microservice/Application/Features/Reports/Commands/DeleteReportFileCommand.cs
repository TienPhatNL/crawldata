using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to delete a report file attachment
/// User explicitly deletes the file (not just replacing)
/// </summary>
public class DeleteReportFileCommand : IRequest<DeleteReportFileResponse>
{
    /// <summary>
    /// Report ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid ReportId { get; set; }
}

/// <summary>
/// Response for report file deletion
/// </summary>
public class DeleteReportFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
