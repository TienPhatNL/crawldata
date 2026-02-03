using MediatR;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class ValidateTopicWeightUpdateQueryHandler : IRequestHandler<ValidateTopicWeightUpdateQuery, TopicWeightValidationResult>
{
    private readonly ITopicWeightValidationService _validationService;
    
    public ValidateTopicWeightUpdateQueryHandler(ITopicWeightValidationService validationService)
    {
        _validationService = validationService;
    }
    
    public async Task<TopicWeightValidationResult> Handle(ValidateTopicWeightUpdateQuery request, CancellationToken cancellationToken)
    {
        return await _validationService.ValidateUpdateAsync(request.Id);
    }
}
