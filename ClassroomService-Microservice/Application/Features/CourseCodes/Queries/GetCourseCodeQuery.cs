using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.CourseCodes.Queries;

/// <summary>
/// Query to get a specific course code by ID
/// </summary>
public class GetCourseCodeQuery : IRequest<GetCourseCodeResponse>
{
    /// <summary>
    /// The course code ID to retrieve
    /// </summary>
    [Required]
    public Guid Id { get; set; }
}