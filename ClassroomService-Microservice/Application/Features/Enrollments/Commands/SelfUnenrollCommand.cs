using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Command for a student to unenroll themselves from a course
/// </summary>
public class SelfUnenrollCommand : IRequest<UnenrollStudentResponse>
{
    /// <summary>
    /// The ID of the course to unenroll from
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid CourseId { get; set; }

    /// <summary>
    /// The student's user ID (will be set automatically from current user)
    /// </summary>
    public Guid StudentId { get; set; }
}