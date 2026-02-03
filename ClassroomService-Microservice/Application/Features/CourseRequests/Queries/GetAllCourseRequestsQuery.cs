using ClassroomService.Application.Features.CourseRequests.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

/// <summary>
/// Query to get all course requests (for staff/admin)
/// </summary>
public class GetAllCourseRequestsQuery : IRequest<GetAllCourseRequestsResponse>
{
    public CourseRequestFilterDto Filter { get; set; } = new();
    public Guid CurrentUserId { get; set; }
    public string CurrentUserRole { get; set; } = string.Empty;
}

public class GetAllCourseRequestsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CourseRequestDto> CourseRequests { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}