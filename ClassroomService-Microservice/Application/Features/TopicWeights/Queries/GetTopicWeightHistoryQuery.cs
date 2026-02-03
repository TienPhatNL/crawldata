using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

/// <summary>
/// Query to get history of a specific TopicWeight
/// </summary>
public class GetTopicWeightHistoryQuery : IRequest<List<TopicWeightHistoryDto>>
{
    public Guid TopicWeightId { get; set; }
}
