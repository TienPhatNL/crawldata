using MediatR;
using ClassroomService.Application.Features.Enrollments.Commands;

namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to join a course with access code
/// </summary>
public class JoinCourseWithCodeCommand : IRequest<EnrollmentResponse>
{
    /// <summary>
    /// Course ID to join
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// Student ID (set from current user context)
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Access code for the course (required if course has access code)
    /// </summary>
    public string? AccessCode { get; set; }
}