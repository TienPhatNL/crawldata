using MediatR;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to delete the syllabus file from a course
/// Only the course lecturer can delete the syllabus
/// </summary>
public class DeleteCourseSyllabusCommand : IRequest<DeleteCourseSyllabusResponse>
{
    /// <summary>
    /// Course ID (from route parameter)
    /// </summary>
    public Guid CourseId { get; set; }
}

/// <summary>
/// Response for course syllabus deletion
/// </summary>
public class DeleteCourseSyllabusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
