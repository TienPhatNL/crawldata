using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

/// <summary>
/// Handler for bulk creating course-specific TopicWeights
/// Special rule: Allows creation even during active terms if course status is PendingApproval
/// </summary>
public class BulkCreateCourseTopicWeightsCommandHandler : IRequestHandler<BulkCreateCourseTopicWeightsCommand, BulkCreateCourseTopicWeightsResponse>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<Assignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;
    private readonly ITopicWeightValidationService _validationService;
    private readonly ITopicWeightHistoryService _historyService;
    private readonly ILogger<BulkCreateCourseTopicWeightsCommandHandler> _logger;

    public BulkCreateCourseTopicWeightsCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<Topic> topicRepository,
        IRepository<Course> courseRepository,
        IRepository<Assignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        TopicWeightValidator validator,
        ITopicWeightValidationService validationService,
        ITopicWeightHistoryService historyService,
        ILogger<BulkCreateCourseTopicWeightsCommandHandler> logger)
    {
        _topicWeightRepository = topicWeightRepository;
        _topicRepository = topicRepository;
        _courseRepository = courseRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _validationService = validationService;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<BulkCreateCourseTopicWeightsResponse> Handle(BulkCreateCourseTopicWeightsCommand request, CancellationToken cancellationToken)
    {
        var response = new BulkCreateCourseTopicWeightsResponse();
        
        if (request.Weights == null || !request.Weights.Any())
        {
            response.Success = false;
            response.Message = "No weights provided";
            return response;
        }

        // VALIDATION 1: Course exists
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            response.Success = false;
            response.Message = $"Course {request.CourseId} not found";
            return response;
        }

        // VALIDATION 2: Check if course has any assignments (CRITICAL SAFEGUARD)
        var existingAssignments = await _assignmentRepository.GetManyAsync(
            a => a.CourseId == request.CourseId,
            cancellationToken);
        
        if (existingAssignments.Any())
        {
            response.Success = false;
            response.Message = "Cannot modify weights: Assignments already created for this course. " +
                             "Changing weights won't affect existing assignment snapshots.";
            _logger.LogWarning(
                "Blocked bulk create for course {CourseId}: Assignments exist", 
                request.CourseId);
            return response;
        }

        // VALIDATION 3: Course-aware term validation
        var validation = await _validationService.ValidateForCourseOperationAsync(
            request.CourseId, 
            TopicWeightOperation.Create);
        
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = validation.ErrorMessage ?? "Validation failed";
            response.Errors.Add(validation.ErrorMessage!);
            return response;
        }

        // Log if bypassing term validation due to PendingApproval status
        if (course.Status == CourseStatus.PendingApproval)
        {
            _logger.LogInformation(
                "Bulk create allowed for course {CourseId} in PendingApproval status (bypassing term validation)",
                request.CourseId);
        }

        // VALIDATION 4: Calculate total weight (must be exactly 100%)
        var totalWeight = request.Weights.Sum(w => w.WeightPercentage);
        if (totalWeight != 100m)
        {
            throw new InvalidOperationException(
                $"Total weight must be exactly 100%. Current total: {totalWeight}%. " +
                "This is a strict requirement for course-specific weights configured by staff.");
        }

        // VALIDATION 5: Validate all weights
        var validationErrors = new List<string>();
        var validatedWeights = new List<(TopicWeightCreateItem Item, Topic Topic)>();

        foreach (var weightItem in request.Weights)
        {
            // Check topic exists
            var topic = await _topicRepository.GetByIdAsync(weightItem.TopicId, cancellationToken);
            if (topic == null)
            {
                validationErrors.Add($"Topic {weightItem.TopicId}: Not found");
                continue;
            }

            // Check for duplicate topics
            if (validatedWeights.Any(v => v.Topic.Id == topic.Id))
            {
                validationErrors.Add($"Topic {topic.Name}: Duplicate topic in request");
                continue;
            }

            // Check if weight already exists for this course + topic
            var existingWeight = await _topicWeightRepository.GetAsync(
                tw => tw.SpecificCourseId == request.CourseId && tw.TopicId == topic.Id,
                cancellationToken);
            
            if (existingWeight != null)
            {
                validationErrors.Add($"Topic {topic.Name}: Weight already exists for this course");
                continue;
            }

            // Validate weight percentage
            if (weightItem.WeightPercentage < 0 || weightItem.WeightPercentage > 100)
            {
                validationErrors.Add($"Topic {topic.Name}: Weight must be between 0 and 100");
                continue;
            }

            validatedWeights.Add((weightItem, topic));
        }

        if (validationErrors.Any())
        {
            response.Success = false;
            response.Message = "Validation failed";
            response.Errors = validationErrors;
            response.FailedCount = request.Weights.Count;
            return response;
        }

        // CREATE ALL WEIGHTS
        var createdWeights = new List<TopicWeight>();
        
        foreach (var (item, topic) in validatedWeights)
        {
            var topicWeight = new TopicWeight
            {
                Id = Guid.NewGuid(),
                TopicId = topic.Id,
                CourseCodeId = null, // Course-specific weight (not CourseCode-level)
                SpecificCourseId = request.CourseId,
                WeightPercentage = item.WeightPercentage,
                Description = item.Description ?? $"Weight for {topic.Name} in course",
                ConfiguredBy = request.ConfiguredBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Validate using domain validator
            var validationResult = _validator.ValidateTopicWeight(topicWeight);
            if (!validationResult.IsValid)
            {
                validationErrors.Add($"Topic {topic.Name}: {validationResult.ErrorMessage}");
                continue;
            }

            await _topicWeightRepository.AddAsync(topicWeight, cancellationToken);
            createdWeights.Add(topicWeight);

            // Record in history
            await _historyService.RecordCreationAsync(
                topicWeight,
                request.ConfiguredBy,
                request.ChangeReason ?? $"Bulk create for course (Status: {course.Status})");
        }

        if (createdWeights.Any())
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build response DTOs
            foreach (var weight in createdWeights)
            {
                var topic = await _topicRepository.GetByIdAsync(weight.TopicId, cancellationToken);
                
                // Use course-aware validation to reflect PendingApproval status correctly
                var weightValidation = await _validationService.ValidateForCourseOperationAsync(
                    request.CourseId, 
                    TopicWeightOperation.Update);

                response.CreatedWeights.Add(new TopicWeightResponseDto
                {
                    Id = weight.Id,
                    TopicId = weight.TopicId,
                    TopicName = topic?.Name ?? "Unknown",
                    CourseCodeId = null,
                    CourseCodeName = null,
                    SpecificCourseId = weight.SpecificCourseId,
                    SpecificCourseName = course.Name,
                    WeightPercentage = weight.WeightPercentage,
                    Description = weight.Description,
                    ConfiguredBy = weight.ConfiguredBy,
                    UpdatedAt = weight.UpdatedAt,
                    CanUpdate = weightValidation.IsValid,
                    CanDelete = weightValidation.IsValid,
                    BlockReason = weightValidation.IsValid ? null : weightValidation.ErrorMessage
                });
            }

            response.Success = true;
            response.Message = $"Successfully created {createdWeights.Count} weight(s) for course";
            response.SuccessCount = createdWeights.Count;
            
            _logger.LogInformation(
                "Bulk created {Count} TopicWeights for course {CourseId} (Status: {Status})",
                createdWeights.Count, request.CourseId, course.Status);
        }
        else
        {
            response.Success = false;
            response.Message = "No weights were created";
            response.Errors = validationErrors;
        }

        return response;
    }
}
