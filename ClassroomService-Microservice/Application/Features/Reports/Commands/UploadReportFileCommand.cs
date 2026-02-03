using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Command to upload a file attachment for a report
/// Only allowed in Draft or RequiresRevision status
/// </summary>
public class UploadReportFileCommand : IRequest<UploadReportFileResponse>
{
    /// <summary>
    /// Report ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// The file to upload (PDF, DOCX, TXT, ZIP, etc.)
    /// </summary>
    [JsonIgnore]
    public IFormFile File { get; set; } = null!;
}

/// <summary>
/// Response for report file upload
/// </summary>
public class UploadReportFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public int? Version { get; set; }
}
