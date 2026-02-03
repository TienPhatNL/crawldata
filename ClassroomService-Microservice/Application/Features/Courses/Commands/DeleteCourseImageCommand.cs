using System.Text.Json.Serialization;
using MediatR;

namespace ClassroomService.Application.Features.Courses.Commands;

/// <summary>
/// Command to delete a course image
/// </summary>
public class DeleteCourseImageCommand : IRequest<DeleteCourseImageResponse>
{
    /// <summary>
    /// Course ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid CourseId { get; set; }
}

/// <summary>
/// Response for course image deletion
/// </summary>
public class DeleteCourseImageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
