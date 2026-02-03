using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightHistoryQueryHandler : IRequestHandler<GetTopicWeightHistoryQuery, List<TopicWeightHistoryDto>>
{
    private readonly ITopicWeightHistoryService _historyService;
    
    public GetTopicWeightHistoryQueryHandler(ITopicWeightHistoryService historyService)
    {
        _historyService = historyService;
    }
    
    public async Task<List<TopicWeightHistoryDto>> Handle(GetTopicWeightHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _historyService.GetHistoryAsync(request.TopicWeightId);
        
        return history.Select(h => new TopicWeightHistoryDto
        {
            Id = h.Id,
            TopicWeightId = h.TopicWeightId,
            TopicId = h.TopicId,
            TopicName = h.Topic?.Name,
            CourseCodeId = h.CourseCodeId,
            CourseCodeName = h.CourseCode?.Code,
            SpecificCourseId = h.SpecificCourseId,
            SpecificCourseName = h.SpecificCourse?.Name,
            TermId = h.TermId,
            TermName = h.TermName,
            OldWeightPercentage = h.OldWeightPercentage,
            NewWeightPercentage = h.NewWeightPercentage,
            ModifiedBy = h.ModifiedBy,
            ModifiedAt = h.ModifiedAt,
            Action = h.Action.ToString(),
            ChangeReason = h.ChangeReason,
            AffectedTerms = h.AffectedTerms
        }).ToList();
    }
}
