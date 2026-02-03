using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class BulkConfigureTopicWeightsCommand : IRequest<List<TopicWeightResponseDto>>
{
    public Guid CourseCodeId { get; set; }
    public List<TopicWeightConfigDto> TopicWeights { get; set; } = new();
    public Guid ConfiguredBy { get; set; } // From authenticated user
}
