using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to inactivate a course (Lecturer only)
/// </summary>
public class InactivateCourseCommand : IRequest<InactivateCourseResponse>
{
    /// <summary>
    /// The course ID to inactivate
    /// </summary>
    [Required]
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// The lecturer ID (set from current user context)
    /// </summary>
    public Guid LecturerId { get; set; }
    
    /// <summary>
    /// Optional reason for inactivating the course
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Response for inactivate course command
/// </summary>
public class InactivateCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
}
