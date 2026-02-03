using ClassroomService.Application.Features.CourseRequests.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CourseRequests.Queries;

/// <summary>
/// Query to get lecturer's own course requests
/// </summary>
public class GetMyCourseRequestsQuery : IRequest<GetMyCourseRequestsResponse>
{
    public CourseRequestFilterDto Filter { get; set; } = new();
    public Guid LecturerId { get; set; }
}

public class GetMyCourseRequestsResponse
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