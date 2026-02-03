using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Commands;

public class UpdateCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public CourseDto? UpdatedCourse { get; set; }
}