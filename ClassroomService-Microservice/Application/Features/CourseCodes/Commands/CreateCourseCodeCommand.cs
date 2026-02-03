using MediatR;
using System.ComponentModel.DataAnnotations;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Command to create a new course code
/// </summary>
public class CreateCourseCodeCommand : IRequest<CreateCourseCodeResponse>
{
    /// <summary>
    /// The course code identifier (e.g., "CS101", "MATH202")
    /// </summary>
    /// <example>CS101</example>
    [Required]
    [StringLength(ValidationConstants.MaxCourseCodeLength, MinimumLength = ValidationConstants.MinCourseCodeLength)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The title/name of the course
    /// </summary>
    /// <example>Introduction to Computer Science</example>
    [Required]
    [StringLength(ValidationConstants.MaxCourseCodeTitleLength, MinimumLength = ValidationConstants.MinCourseCodeTitleLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the course content and objectives
    /// </summary>
    /// <example>This course introduces fundamental concepts of computer programming using C#.</example>
    [StringLength(ValidationConstants.MaxCourseCodeDescriptionLength)]
    public string? Description { get; set; }

    /// <summary>
    /// The academic department this course belongs to
    /// </summary>
    /// <example>Computer Science</example>
    [Required]
    [StringLength(ValidationConstants.MaxDepartmentLength, MinimumLength = ValidationConstants.MinDepartmentLength)]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Whether this course code is currently active
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; } = true;
}