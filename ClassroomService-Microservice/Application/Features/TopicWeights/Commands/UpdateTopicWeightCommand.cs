using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class UpdateTopicWeightCommand : IRequest<TopicWeightResponseDto>
{
    public Guid Id { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
    public Guid ConfiguredBy { get; set; } // From authenticated user
    public string? ChangeReason { get; set; } // Optional reason for updating this weight
}
