using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightsForCourseCodeQuery : IRequest<List<TopicWeightResponseDto>>
{
    public Guid CourseCodeId { get; set; }
}
