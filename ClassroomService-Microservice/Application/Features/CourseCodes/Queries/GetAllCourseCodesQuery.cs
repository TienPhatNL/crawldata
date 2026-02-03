using MediatR;
using ClassroomService.Application.Features.CourseCodes.DTOs;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Query to get all course codes with filtering and pagination
/// </summary>
public class GetAllCourseCodesQuery : IRequest<GetAllCourseCodesResponse>
{
    /// <summary>
    /// Filter and pagination options
    /// </summary>
    public CourseCodeFilterDto Filter { get; set; } = new();
}