using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to create a new course
/// </summary>
public class CreateCourseCommand : IRequest<CreateCourseResponse>
{
    /// <summary>
    /// The CourseCode ID to use for this course
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid CourseCodeId { get; set; }

    /// <summary>
    /// Course description/details
    /// </summary>
    /// <example>Introduction to programming concepts using C#</example>
    [Required]
    [StringLength(ValidationConstants.MaxCourseDescriptionLength, MinimumLength = ValidationConstants.MinCourseDescriptionLength)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Term ID (reference to Term entity)
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid TermId { get; set; }

    /// <summary>
    /// The lecturer/instructor ID for this course (set internally, not from request)
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    [JsonIgnore]
    public Guid LecturerId { get; set; }

    /// <summary>
    /// Whether this course requires an access code to join
    /// </summary>
    /// <example>true</example>
    public bool RequiresAccessCode { get; set; } = false;

    /// <summary>
    /// Course announcement/forum
    /// </summary>
    /// <example>Welcome to CS101! Check this forum regularly for updates.</example>
    [StringLength(2000)]
    public string? Announcement { get; set; }

    /// <summary>
    /// Type of access code to generate (if RequiresAccessCode is true)
    /// </summary>
    /// <example>AlphaNumeric</example>
    public AccessCodeType AccessCodeType { get; set; } = AccessCodeType.AlphaNumeric;

    /// <summary>
    /// Custom access code (only used if AccessCodeType is Custom)
    /// </summary>
    /// <example>CUSTOM123</example>
    public string? CustomAccessCode { get; set; }

    /// <summary>
    /// When the access code expires (optional)
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTime? AccessCodeExpiresAt { get; set; }
}