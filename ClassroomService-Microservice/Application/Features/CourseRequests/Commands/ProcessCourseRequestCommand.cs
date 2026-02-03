using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ClassroomService.Domain.Enums;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command for staff to process (approve/reject) course request
/// </summary>
public class ProcessCourseRequestCommand : IRequest<ProcessCourseRequestResponse>
{
    /// <summary>
    /// The course request ID
    /// </summary>
    [Required]
    [JsonIgnore]
    public Guid CourseRequestId { get; set; }
    
    /// <summary>
    /// Whether to approve or reject the request
    /// </summary>
    [Required]
    public CourseRequestStatus Status { get; set; }
    
    /// <summary>
    /// Comments from staff member
    /// </summary>
    [StringLength(500)]
    public string? ProcessingComments { get; set; }
    
    /// <summary>
    /// The staff member ID (set internally, not from request)
    /// </summary>
    [JsonIgnore]
    public Guid ProcessedBy { get; set; }
}