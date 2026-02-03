using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class UpdateTopicWeightCommandHandler : IRequestHandler<UpdateTopicWeightCommand, TopicWeightResponseDto>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<CourseCode> _courseCodeRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;
    private readonly ITopicWeightValidationService _validationService;
    private readonly ITopicWeightHistoryService _historyService;

    public UpdateTopicWeightCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<Topic> topicRepository,
        IRepository<CourseCode> courseCodeRepository,
        IRepository<Course> courseRepository,
        IUnitOfWork unitOfWork,
        TopicWeightValidator validator,
        ITopicWeightValidationService validationService,
        ITopicWeightHistoryService historyService)
    {
        _topicWeightRepository = topicWeightRepository;
        _topicRepository = topicRepository;
        _courseCodeRepository = courseCodeRepository;
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _validationService = validationService;
        _historyService = historyService;
    }

    public async Task<TopicWeightResponseDto> Handle(UpdateTopicWeightCommand request, CancellationToken cancellationToken)
    {
        // STEP 1: Validate term status before allowing update
        var validation = await _validationService.ValidateUpdateAsync(request.Id);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        var topicWeight = await _topicWeightRepository.GetByIdAsync(request.Id, cancellationToken);

        if (topicWeight == null)
            throw new KeyNotFoundException($"TopicWeight with ID {request.Id} not found");

        // Save old weight for history tracking
        var oldWeight = topicWeight.WeightPercentage;

        // Get related entities for response
        var topic = await _topicRepository.GetByIdAsync(topicWeight.TopicId, cancellationToken);
        CourseCode? courseCode = null;
        Course? course = null;

        if (topicWeight.CourseCodeId.HasValue)
        {
            courseCode = await _courseCodeRepository.GetByIdAsync(topicWeight.CourseCodeId.Value, cancellationToken);
        }

        if (topicWeight.SpecificCourseId.HasValue)
        {
            course = await _courseRepository.GetByIdAsync(topicWeight.SpecificCourseId.Value, cancellationToken);
        }

        // Validate total weight won't exceed 100% (considering both CourseCode and SpecificCourse weights)
        decimal totalWeight;
        
        if (topicWeight.SpecificCourseId.HasValue)
        {
            // For SpecificCourse weight: check effective total (CourseCode defaults + SpecificCourse overrides)
            // Get the course's CourseCodeId
            if (course?.CourseCodeId == null)
                throw new InvalidOperationException("Course must have a CourseCodeId");

            // Get all CourseCode-level weights for this course's code
            var courseCodeWeights = await _topicWeightRepository.GetManyAsync(
                tw => tw.CourseCodeId == course.CourseCodeId && tw.SpecificCourseId == null,
                cancellationToken);

            // Get all SpecificCourse-level weights for this specific course (excluding the one being updated)
            var specificCourseWeights = await _topicWeightRepository.GetManyAsync(
                tw => tw.SpecificCourseId == topicWeight.SpecificCourseId && tw.Id != request.Id,
                cancellationToken);

            // Calculate effective total:
            // 1. Start with all CourseCode weights
            var effectiveWeights = courseCodeWeights.ToDictionary(tw => tw.TopicId, tw => tw.WeightPercentage);

            // 2. Override with existing SpecificCourse weights
            foreach (var scWeight in specificCourseWeights)
            {
                effectiveWeights[scWeight.TopicId] = scWeight.WeightPercentage;
            }

            // 3. Override/add the weight being updated
            effectiveWeights[topicWeight.TopicId] = request.WeightPercentage;

            totalWeight = effectiveWeights.Values.Sum();
            
            if (totalWeight > 100m)
            {
                throw new InvalidOperationException(
                    $"Effective total weight for this course cannot exceed 100%. " +
                    $"CourseCode defaults: {courseCodeWeights.Sum(tw => tw.WeightPercentage)}%, " +
                    $"SpecificCourse overrides (including this update): {effectiveWeights.Values.Sum()}%, " +
                    $"resulting in {totalWeight}% total");
            }
        }
        else if (topicWeight.CourseCodeId.HasValue)
        {
            // For CourseCode weight: only check other CourseCode-level weights
            var relatedWeights = await _topicWeightRepository.GetManyAsync(
                tw => tw.CourseCodeId == topicWeight.CourseCodeId 
                    && tw.SpecificCourseId == null 
                    && tw.Id != request.Id,
                cancellationToken);

            totalWeight = relatedWeights.Sum(tw => tw.WeightPercentage) + request.WeightPercentage;
            
            if (totalWeight > 100m)
            {
                throw new InvalidOperationException(
                    $"Total weight for CourseCode cannot exceed 100%. " +
                    $"Current total of other weights: {relatedWeights.Sum(tw => tw.WeightPercentage)}%. " +
                    $"Adding {request.WeightPercentage}% would result in {totalWeight}%");
            }
        }

        // Update properties
        topicWeight.WeightPercentage = request.WeightPercentage;
        topicWeight.Description = request.Description;
        topicWeight.ConfiguredBy = request.ConfiguredBy;
        topicWeight.UpdatedAt = DateTime.UtcNow;

        // Validate
        var validationResult = _validator.ValidateTopicWeight(topicWeight);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(validationResult.ErrorMessage);

        await _topicWeightRepository.UpdateAsync(topicWeight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Record update in history
        await _historyService.RecordUpdateAsync(
            topicWeight,
            oldWeight,
            request.ConfiguredBy,
            request.ChangeReason);

        // Re-check validation for response
        var finalValidation = await _validationService.ValidateUpdateAsync(topicWeight.Id);

        return new TopicWeightResponseDto
        {
            Id = topicWeight.Id,
            TopicId = topicWeight.TopicId,
            TopicName = topic?.Name ?? string.Empty,
            CourseCodeId = topicWeight.CourseCodeId,
            CourseCodeName = courseCode?.Code,
            SpecificCourseId = topicWeight.SpecificCourseId,
            SpecificCourseName = course?.Name,
            WeightPercentage = topicWeight.WeightPercentage,
            Description = topicWeight.Description,
            ConfiguredBy = topicWeight.ConfiguredBy,
            ConfiguredAt = topicWeight.CreatedAt,
            UpdatedAt = topicWeight.UpdatedAt,
            CanUpdate = finalValidation.IsValid,
            CanDelete = finalValidation.IsValid,
            BlockReason = finalValidation.IsValid ? null : finalValidation.ErrorMessage
        };
    }
}
