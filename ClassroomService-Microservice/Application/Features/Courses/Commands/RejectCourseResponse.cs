using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Commands;

public class RejectCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CourseDto? Course { get; set; }
}
