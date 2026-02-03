using MediatR;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

/// <summary>
/// Query to validate if a TopicWeight can be updated
/// </summary>
public class ValidateTopicWeightUpdateQuery : IRequest<TopicWeightValidationResult>
{
    public Guid Id { get; set; }
}
