using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

/// <summary>
/// Command to hard delete a course request (Lecturer only, Pending state only)
/// </summary>
public class DeleteCourseRequestCommand : IRequest<DeleteCourseRequestResponse>
{
    /// <summary>
    /// The course request ID to delete
    /// </summary>
    public Guid CourseRequestId { get; set; }
    
    /// <summary>
    /// The lecturer ID (set from current user context)
    /// </summary>
    public Guid LecturerId { get; set; }
}

/// <summary>
/// Response for delete course request command
/// </summary>
public class DeleteCourseRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CourseRequestId { get; set; }
}
