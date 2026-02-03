using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to unenroll a student from a course
/// </summary>
public class UnenrollStudentCommand : IRequest<UnenrollStudentResponse>
{
    /// <summary>
    /// The ID of the course to unenroll from
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid CourseId { get; set; }

    /// <summary>
    /// The ID of the student to unenroll
    /// </summary>
    /// <example>87654321-4321-4321-4321-210987654321</example>
    [Required]
    public Guid StudentId { get; set; }

    /// <summary>
    /// Reason for unenrollment (optional)
    /// </summary>
    /// <example>Student withdrew from course</example>
    public string? Reason { get; set; }

    /// <summary>
    /// ID of the user who is performing the unenrollment (admin, lecturer, or student themselves)
    /// </summary>
    public Guid? UnenrolledBy { get; set; }
}