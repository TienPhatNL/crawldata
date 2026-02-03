using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get a specific course by ID
/// </summary>
public class GetCourseQuery : IRequest<GetCourseResponse>
{
    /// <summary>
    /// The unique identifier of the course
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid CourseId { get; set; }

    /// <summary>
    /// Current user ID for access control (optional - can be null for anonymous access)
    /// </summary>
    public Guid? CurrentUserId { get; set; }

    /// <summary>
    /// Current user role for access control (optional)
    /// </summary>
    public string? CurrentUserRole { get; set; }
}