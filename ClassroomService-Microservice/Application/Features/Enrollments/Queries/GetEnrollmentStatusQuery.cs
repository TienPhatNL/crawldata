using MediatR;

namespace ClassroomService.Application.Features.Enrollments.Queries;

/// <summary>
/// Query to check if a student is enrolled in a course
/// </summary>
public class GetEnrollmentStatusQuery : IRequest<EnrollmentStatusResponse>
{
    /// <summary>
    /// The ID of the course to check
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// The ID of the student to check
    /// </summary>
    public Guid StudentId { get; set; }
}