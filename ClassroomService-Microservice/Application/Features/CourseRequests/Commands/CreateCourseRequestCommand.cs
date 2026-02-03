using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command for lecturer to request course creation
/// </summary>
public class CreateCourseRequestCommand : IRequest<CreateCourseRequestResponse>
{
    /// <summary>
    /// The CourseCode ID to request
    /// </summary>
    [Required]
    public Guid CourseCodeId { get; set; }
    
    /// <summary>
    /// Course description/details
    /// </summary>
    [Required]
    [StringLength(ValidationConstants.MaxCourseDescriptionLength, MinimumLength = ValidationConstants.MinCourseDescriptionLength)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Term ID (reference to Term entity)
    /// </summary>
    [Required]
    public Guid TermId { get; set; }
    
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