using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightsForCourseQueryHandler : IRequestHandler<GetTopicWeightsForCourseQuery, List<TopicWeightResponseDto>>
{
    private readonly ClassroomDbContext _context;
    private readonly ITopicWeightValidationService _validationService;

    public GetTopicWeightsForCourseQueryHandler(
        ClassroomDbContext context,
        ITopicWeightValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<List<TopicWeightResponseDto>> Handle(GetTopicWeightsForCourseQuery request, CancellationToken cancellationToken)
    {
        // Get the course with its course code to find applicable weights
        var course = await _context.Courses
            .Include(c => c.CourseCode)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        if (course == null)
        {
            throw new KeyNotFoundException($"Course with ID {request.CourseId} not found");
        }

        // Get weights for this specific course OR weights for the course code (fallback)
        // Only get course-specific weights (not course code fallback)
        var weights = await _context.TopicWeights
            .Include(tw => tw.Topic)
            .Include(tw => tw.CourseCode)
            .Include(tw => tw.SpecificCourse)
            .Where(tw => tw.SpecificCourseId == request.CourseId)
            .OrderBy(tw => tw.Topic.Name)
            .ToListAsync(cancellationToken);

        // Map to DTOs with validation status
        var dtos = new List<TopicWeightResponseDto>();
        foreach (var tw in weights)
        {
            // Use course-aware validation to respect PendingApproval status
            var validation = await _validationService.ValidateForCourseOperationAsync(
                request.CourseId, 
                TopicWeightOperation.Update);

            var dto = new TopicWeightResponseDto
            {
                Id = tw.Id,
                TopicId = tw.TopicId,
                TopicName = tw.Topic.Name,
                CourseCodeId = tw.CourseCodeId,
                CourseCodeName = tw.CourseCode?.Code,
                SpecificCourseId = tw.SpecificCourseId,
                SpecificCourseName = tw.SpecificCourse?.Name,
                WeightPercentage = tw.WeightPercentage,
                Description = tw.Description,
                ConfiguredBy = tw.ConfiguredBy,
                ConfiguredAt = tw.CreatedAt,
                UpdatedAt = tw.UpdatedAt,
                CanUpdate = validation.IsValid,
                CanDelete = validation.IsValid,
                BlockReason = validation.IsValid ? null : validation.ErrorMessage
            };

            dtos.Add(dto);
        }

        return dtos;
    }
}
