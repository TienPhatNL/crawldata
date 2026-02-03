using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Courses.Queries;

/// <summary>
/// Query to get course statistics
/// </summary>
public class GetCourseStatisticsQuery : IRequest<GetCourseStatisticsResponse>
{
    /// <summary>
    /// The ID of the course to get statistics for
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    [Required]
    public Guid CourseId { get; set; }
}