using ClassroomService.Application.Features.CourseRequests.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

/// <summary>
/// Query to get a single course request by ID
/// </summary>
public class GetCourseRequestQuery : IRequest<GetCourseRequestResponse>
{
    public Guid CourseRequestId { get; set; }
    public Guid CurrentUserId { get; set; }
    public string CurrentUserRole { get; set; } = string.Empty;
}

public class GetCourseRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CourseRequestDto? CourseRequest { get; set; }
}
