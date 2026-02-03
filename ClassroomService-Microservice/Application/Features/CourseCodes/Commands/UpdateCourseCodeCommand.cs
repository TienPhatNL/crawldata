using MediatR;
using System.ComponentModel.DataAnnotations;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Command to update an existing course code
/// </summary>
public class UpdateCourseCodeCommand : IRequest<UpdateCourseCodeResponse>
{
    /// <summary>
    /// The course code ID to update
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Updated course code identifier
    /// </summary>
    [StringLength(ValidationConstants.MaxCourseCodeLength, MinimumLength = ValidationConstants.MinCourseCodeLength)]
    public string? Code { get; set; }

    /// <summary>
    /// Updated course title
    /// </summary>
    [StringLength(ValidationConstants.MaxCourseCodeTitleLength, MinimumLength = ValidationConstants.MinCourseCodeTitleLength)]
    public string? Title { get; set; }

    /// <summary>
    /// Updated course description
    /// </summary>
    [StringLength(ValidationConstants.MaxCourseCodeDescriptionLength)]
    public string? Description { get; set; }

    /// <summary>
    /// Updated department
    /// </summary>
    [StringLength(ValidationConstants.MaxDepartmentLength, MinimumLength = ValidationConstants.MinDepartmentLength)]
    public string? Department { get; set; }

    /// <summary>
    /// Updated active status
    /// </summary>
    public bool? IsActive { get; set; }
}