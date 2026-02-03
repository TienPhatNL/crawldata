using ClassroomService.Application.Features.CourseRequests.DTOs;
using ClassroomService.Application.Features.Courses.Queries;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

public class ProcessCourseRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CourseRequestDto? CourseRequest { get; set; }
    public CourseDto? CreatedCourse { get; set; }
}