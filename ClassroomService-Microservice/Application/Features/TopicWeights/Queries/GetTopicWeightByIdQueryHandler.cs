using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetTopicWeightByIdQueryHandler : IRequestHandler<GetTopicWeightByIdQuery, TopicWeightResponseDto>
{
    private readonly ClassroomDbContext _context;
    private readonly ITopicWeightValidationService _validationService;

    public GetTopicWeightByIdQueryHandler(
        ClassroomDbContext context,
        ITopicWeightValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<TopicWeightResponseDto> Handle(GetTopicWeightByIdQuery request, CancellationToken cancellationToken)
    {
        var tw = await _context.TopicWeights
            .Include(tw => tw.Topic)
            .Include(tw => tw.CourseCode)
            .Include(tw => tw.SpecificCourse)
            .FirstOrDefaultAsync(tw => tw.Id == request.Id, cancellationToken);

        if (tw == null)
            throw new KeyNotFoundException($"TopicWeight with ID {request.Id} not found");

        // Check if this weight can be updated/deleted
        var validation = await _validationService.ValidateUpdateAsync(tw.Id);

        return new TopicWeightResponseDto
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
    }
}
