using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get a specific course by its unique code
/// </summary>
public class GetCourseByUniqueCodeQuery : IRequest<GetCourseResponse>
{
    /// <summary>
    /// The unique 6-character code of the course
    /// </summary>
    /// <example>A1B2C3</example>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string UniqueCode { get; set; } = string.Empty;

    /// <summary>
    /// Current user ID for access control (optional - can be null for anonymous access)
    /// </summary>
    public Guid? CurrentUserId { get; set; }

    /// <summary>
    /// Current user role for access control (optional)
    /// </summary>
    public string? CurrentUserRole { get; set; }
}
