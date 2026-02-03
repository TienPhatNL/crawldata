using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command for lecturer to update a course request (only in Pending status)
/// </summary>
public class UpdateCourseRequestCommand : IRequest<UpdateCourseRequestResponse>
{
    /// <summary>
    /// The course request ID to update
    /// </summary>
    [JsonIgnore]
    public Guid CourseRequestId { get; set; }
    
    /// <summary>
    /// The CourseCode ID to request
    /// </summary>
    public Guid? CourseCodeId { get; set; }
    
    /// <summary>
    /// Course description/details
    /// </summary>
    [StringLength(ValidationConstants.MaxCourseDescriptionLength, MinimumLength = ValidationConstants.MinCourseDescriptionLength)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Term ID (reference to Term entity)
    /// </summary>
    public Guid? TermId { get; set; }
    
    /// <summary>
    /// Reason for requesting the course
    /// </summary>
    [StringLength(500)]
    public string? RequestReason { get; set; }
    
    /// <summary>
    /// Course announcement/forum
    /// </summary>
    [StringLength(2000)]
    public string? Announcement { get; set; }
    
    /// <summary>
    /// The lecturer ID (set internally, not from request)
    /// </summary>
    [JsonIgnore]
    public Guid LecturerId { get; set; }
}

/// <summary>
/// Response for update course request command
/// </summary>
public class UpdateCourseRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CourseRequestId { get; set; }
}
