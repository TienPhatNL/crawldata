using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

/// <summary>
/// Command to delete a specific file attachment from an assignment
/// </summary>
public class DeleteAssignmentFileCommand : IRequest<DeleteAssignmentFileResponse>
{
    /// <summary>
    /// Assignment ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid AssignmentId { get; set; }
    
    /// <summary>
    /// File attachment ID to delete (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid FileId { get; set; }
}

/// <summary>
/// Response for assignment file deletion
/// </summary>
public class DeleteAssignmentFileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
