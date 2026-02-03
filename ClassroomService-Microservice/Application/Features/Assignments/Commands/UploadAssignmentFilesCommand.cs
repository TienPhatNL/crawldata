using System.Text.Json.Serialization;
using ClassroomService.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Assignments.Commands;

/// <summary>
/// Command to upload multiple file attachments to an assignment
/// </summary>
public class UploadAssignmentFilesCommand : IRequest<UploadAssignmentFilesResponse>
{
    /// <summary>
    /// Assignment ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid AssignmentId { get; set; }
    
    /// <summary>
    /// The files to upload (instructions, reference materials, etc.)
    /// </summary>
    [JsonIgnore]
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
}

/// <summary>
/// Response for assignment files upload
/// </summary>
public class UploadAssignmentFilesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// List of uploaded attachments with metadata
    /// </summary>
    public List<AttachmentMetadata>? UploadedFiles { get; set; }
    
    /// <summary>
    /// Count of successfully uploaded files
    /// </summary>
    public int UploadedCount { get; set; }
}
