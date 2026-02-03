using MediatR;

namespace ClassroomService.Application.Features.Groups.Queries;

/// <summary>
/// Query to get groups that the current student belongs to, optionally filtered by course
/// </summary>
public class GetMyGroupsQuery : IRequest<GetMyGroupsResponse>
{
    /// <summary>
    /// The student user ID
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Optional course ID to filter groups by specific course
    /// </summary>
    public Guid? CourseId { get; set; }
}
