using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

/// <summary>
/// Command to delete a course code
/// </summary>
public class DeleteCourseCodeCommand : IRequest<DeleteCourseCodeResponse>
{
    /// <summary>
    /// The course code ID to delete
    /// </summary>
    [Required]
    public Guid Id { get; set; }
}