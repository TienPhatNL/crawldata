using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class BulkConfigureTopicWeightsCommandHandler : IRequestHandler<BulkConfigureTopicWeightsCommand, List<TopicWeightResponseDto>>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<CourseCode> _courseCodeRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;

    public BulkConfigureTopicWeightsCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<CourseCode> courseCodeRepository,
        IRepository<Topic> topicRepository,
        IUnitOfWork unitOfWork,
        TopicWeightValidator validator)
    {
        _topicWeightRepository = topicWeightRepository;
        _courseCodeRepository = courseCodeRepository;
        _topicRepository = topicRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<List<TopicWeightResponseDto>> Handle(BulkConfigureTopicWeightsCommand request, CancellationToken cancellationToken)
    {
        // Validate CourseCode exists
        var courseCode = await _courseCodeRepository.GetAsync(
            cc => cc.Id == request.CourseCodeId && cc.IsActive,
            cancellationToken);
        
        if (courseCode == null)
            throw new KeyNotFoundException($"CourseCode with ID {request.CourseCodeId} not found or inactive");

        // Validate all topics exist
        var topicIds = request.TopicWeights.Select(tw => tw.TopicId).ToList();
        var topics = await _topicRepository.GetManyAsync(
            t => topicIds.Contains(t.Id) && t.IsActive,
            cancellationToken);

        var topicDict = topics.ToDictionary(t => t.Id);

        if (topicDict.Count != topicIds.Count)
        {
            var missingIds = topicIds.Except(topicDict.Keys);
            throw new KeyNotFoundException($"Some topics not found or inactive: {string.Join(", ", missingIds)}");
        }

        // Validate total weight equals 100%
        var totalWeight = request.TopicWeights.Sum(tw => tw.WeightPercentage);
        if (totalWeight != 100m)
        {
            throw new InvalidOperationException($"Total weight must equal 100%. Current total: {totalWeight}%");
        }

        // Remove existing TopicWeights for this CourseCode
        var existingWeights = await _topicWeightRepository.GetManyAsync(
            tw => tw.CourseCodeId == request.CourseCodeId && tw.SpecificCourseId == null,
            cancellationToken);

        await _topicWeightRepository.DeleteRangeAsync(existingWeights, cancellationToken);

        // Create new TopicWeights
        var newWeights = new List<TopicWeight>();
        foreach (var config in request.TopicWeights)
        {
            var topicWeight = new TopicWeight
            {
                Id = Guid.NewGuid(),
                TopicId = config.TopicId,
                CourseCodeId = request.CourseCodeId,
                SpecificCourseId = null,
                WeightPercentage = config.WeightPercentage,
                Description = config.Description,
                ConfiguredBy = request.ConfiguredBy
            };

            // Validate each weight
            var validationResult = _validator.ValidateTopicWeight(topicWeight);
            if (!validationResult.IsValid)
                throw new InvalidOperationException($"Invalid weight for topic {config.TopicId}: {validationResult.ErrorMessage}");

            newWeights.Add(topicWeight);
        }

        await _topicWeightRepository.AddRangeAsync(newWeights, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Build response
        var response = new List<TopicWeightResponseDto>();
        foreach (var weight in newWeights)
        {
            response.Add(new TopicWeightResponseDto
            {
                Id = weight.Id,
                TopicId = weight.TopicId,
                TopicName = topicDict[weight.TopicId].Name,
                CourseCodeId = weight.CourseCodeId,
                CourseCodeName = courseCode.Code,
                SpecificCourseId = weight.SpecificCourseId,
                SpecificCourseName = null,
                WeightPercentage = weight.WeightPercentage,
                Description = weight.Description,
                ConfiguredBy = weight.ConfiguredBy,
                ConfiguredAt = weight.CreatedAt,
                UpdatedAt = weight.UpdatedAt
            });
        }

        return response;
    }
}
