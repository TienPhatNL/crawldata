using MediatR;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentStatisticsQuery : IRequest<GetAssignmentStatisticsResponse>
{
    public Guid CourseId { get; set; }
}
