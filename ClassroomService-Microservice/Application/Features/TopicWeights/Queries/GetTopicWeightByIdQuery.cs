using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightByIdQuery : IRequest<TopicWeightResponseDto>
{
    public Guid Id { get; set; }
}
