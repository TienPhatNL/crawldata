using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightsForCourseCodeQueryHandler : IRequestHandler<GetTopicWeightsForCourseCodeQuery, List<TopicWeightResponseDto>>
{
    private readonly ClassroomDbContext _context;

    public GetTopicWeightsForCourseCodeQueryHandler(ClassroomDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopicWeightResponseDto>> Handle(GetTopicWeightsForCourseCodeQuery request, CancellationToken cancellationToken)
    {
        var weights = await _context.TopicWeights
            .Include(tw => tw.Topic)
            .Include(tw => tw.CourseCode)
            .Where(tw => tw.CourseCodeId == request.CourseCodeId)
            .OrderBy(tw => tw.Topic.Name)
            .Select(tw => new TopicWeightResponseDto
            {
                Id = tw.Id,
                TopicId = tw.TopicId,
                TopicName = tw.Topic.Name,
                CourseCodeId = tw.CourseCodeId,
                CourseCodeName = tw.CourseCode!.Code,
                SpecificCourseId = tw.SpecificCourseId,
                SpecificCourseName = tw.SpecificCourse != null ? tw.SpecificCourse.Name : null,
                WeightPercentage = tw.WeightPercentage,
                Description = tw.Description,
                ConfiguredBy = tw.ConfiguredBy,
                ConfiguredAt = tw.CreatedAt,
                UpdatedAt = tw.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return weights;
    }
}
