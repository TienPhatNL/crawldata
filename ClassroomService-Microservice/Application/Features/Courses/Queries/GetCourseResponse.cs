namespace ClassroomService.Application.Features.Courses.Queries;

public class GetCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public CourseDto? Course { get; set; }
    
    /// <summary>
    /// Indicates if the requesting student is enrolled in the course (only for students)
    /// </summary>
    public bool? IsEnrolled { get; set; }
}