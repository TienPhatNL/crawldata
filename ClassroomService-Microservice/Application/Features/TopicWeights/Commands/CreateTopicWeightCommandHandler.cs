using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class CreateTopicWeightCommandHandler : IRequestHandler<CreateTopicWeightCommand, TopicWeightResponseDto>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<CourseCode> _courseCodeRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;
    private readonly ITopicWeightHistoryService _historyService;
    private readonly ITopicWeightValidationService _validationService;

    public CreateTopicWeightCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<Topic> topicRepository,
        IRepository<CourseCode> courseCodeRepository,
        IRepository<Course> courseRepository,
        IUnitOfWork unitOfWork,
        TopicWeightValidator validator,
        ITopicWeightHistoryService historyService,
        ITopicWeightValidationService validationService)
    {
        _topicWeightRepository = topicWeightRepository;
        _topicRepository = topicRepository;
        _courseCodeRepository = courseCodeRepository;
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _historyService = historyService;
        _validationService = validationService;
    }

    public async Task<TopicWeightResponseDto> Handle(CreateTopicWeightCommand request, CancellationToken cancellationToken)
    {
        // Validate if creation is allowed (no past/active terms)
        var validation = await _validationService.ValidateCreateAsync(
            request.CourseCodeId, 
            request.SpecificCourseId);
        
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);
        
        // Validate topic exists
        var topic = await _topicRepository.GetAsync(
            t => t.Id == request.TopicId && t.IsActive,
            cancellationToken);
        
        if (topic == null)
            throw new KeyNotFoundException($"Topic with ID {request.TopicId} not found or inactive");

        // Validate CourseCode or SpecificCourse exists
        CourseCode? courseCode = null;
        Course? course = null;

        if (request.CourseCodeId.HasValue)
        {
            courseCode = await _courseCodeRepository.GetAsync(
                cc => cc.Id == request.CourseCodeId.Value && cc.IsActive,
                cancellationToken);
            
            if (courseCode == null)
                throw new KeyNotFoundException($"CourseCode with ID {request.CourseCodeId} not found or inactive");
        }

        if (request.SpecificCourseId.HasValue)
        {
            course = await _courseRepository.GetByIdAsync(request.SpecificCourseId.Value, cancellationToken);
            
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {request.SpecificCourseId} not found");
        }

        // Create TopicWeight entity
        var topicWeight = new TopicWeight
        {
            Id = Guid.NewGuid(),
            TopicId = request.TopicId,
            CourseCodeId = request.CourseCodeId,
            SpecificCourseId = request.SpecificCourseId,
            WeightPercentage = request.WeightPercentage,
            Description = request.Description,
            ConfiguredBy = request.ConfiguredBy
        };

        // Validate using domain validator
        var validationResult = _validator.ValidateTopicWeight(topicWeight);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(validationResult.ErrorMessage);

        // Check for duplicate configuration
        var duplicate = await _topicWeightRepository.ExistsAsync(
            tw => tw.TopicId == request.TopicId 
                && tw.CourseCodeId == request.CourseCodeId 
                && tw.SpecificCourseId == request.SpecificCourseId,
            cancellationToken);

        if (duplicate)
            throw new InvalidOperationException("A TopicWeight configuration already exists for this Topic and CourseCode/Course combination");

        // Calculate total weight after adding this new weight
        string? warningMessage = null;
        if (request.CourseCodeId.HasValue)
        {
            var existingWeights = await _topicWeightRepository.GetManyAsync(
                tw => tw.CourseCodeId == request.CourseCodeId.Value && tw.SpecificCourseId == null,
                cancellationToken);
            
            var totalWeight = existingWeights.Sum(tw => tw.WeightPercentage) + request.WeightPercentage;
            
            if (totalWeight > 100m)
            {
                throw new InvalidOperationException($"Total weight cannot exceed 100%. Adding this weight would result in {totalWeight}%");
            }
            
            if (totalWeight < 100m)
            {
                warningMessage = $"Warning: Total weight is {totalWeight}%, which is less than 100%. Consider adding more topic weights to reach 100%.";
            }
        }

        await _topicWeightRepository.AddAsync(topicWeight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Record creation in history
        await _historyService.RecordCreationAsync(
            topicWeight,
            request.ConfiguredBy,
            request.ChangeReason);

        // Check validation status for response
        var responseValidation = await _validationService.ValidateUpdateAsync(topicWeight.Id);

        return new TopicWeightResponseDto
        {
            Id = topicWeight.Id,
            TopicId = topicWeight.TopicId,
            TopicName = topic.Name,
            CourseCodeId = topicWeight.CourseCodeId,
            CourseCodeName = courseCode?.Code,
            SpecificCourseId = topicWeight.SpecificCourseId,
            SpecificCourseName = course?.Name,
            WeightPercentage = topicWeight.WeightPercentage,
            Description = topicWeight.Description,
            ConfiguredBy = topicWeight.ConfiguredBy,
            ConfiguredAt = topicWeight.CreatedAt,
            UpdatedAt = topicWeight.UpdatedAt,
            CanUpdate = responseValidation.IsValid,
            CanDelete = responseValidation.IsValid,
            BlockReason = responseValidation.IsValid ? null : responseValidation.ErrorMessage,
            Warning = warningMessage
        };
    }
}
