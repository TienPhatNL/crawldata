using ClassroomService.Application.Features.CourseRequests.DTOs;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

public class CreateCourseRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CourseRequestId { get; set; }
    public CourseRequestDto? CourseRequest { get; set; }
}