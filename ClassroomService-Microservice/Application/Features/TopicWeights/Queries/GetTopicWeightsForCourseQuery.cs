using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightsForCourseQuery : IRequest<List<TopicWeightResponseDto>>
{
    public Guid CourseId { get; set; }
}
