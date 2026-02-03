using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Command to randomize all available students in a course into groups
/// </summary>
public class RandomizeStudentsToGroupsCommand : IRequest<RandomizeStudentsToGroupsResponse>
{
    public Guid CourseId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "GroupSize must be greater than 0")]
    public int GroupSize { get; set; }
}
