using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.Courses.Commands;

public class CreateCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid? CourseId { get; set; }
    public CourseDto? Course { get; set; }
}