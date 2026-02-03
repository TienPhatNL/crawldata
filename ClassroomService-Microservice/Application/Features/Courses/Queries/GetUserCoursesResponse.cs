namespace ClassroomService.Application.Features.Courses.Queries;

public class GetUserCoursesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<CourseDto> Courses { get; set; } = new();
    public int TotalCount { get; set; }
}