using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command to delete the syllabus file from a course request
/// Only the requesting lecturer can delete the syllabus
/// </summary>
public class DeleteCourseRequestSyllabusCommand : IRequest<DeleteCourseRequestSyllabusResponse>
{
    /// <summary>
    /// CourseRequest ID (from route parameter)
    /// </summary>
    public Guid CourseRequestId { get; set; }
}

/// <summary>
/// Response for course request syllabus deletion
/// </summary>
public class DeleteCourseRequestSyllabusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
