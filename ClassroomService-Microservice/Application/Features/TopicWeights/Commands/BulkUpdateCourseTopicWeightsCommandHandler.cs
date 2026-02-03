using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

/// <summary>
/// Handler for bulk updating course-specific TopicWeights (partial updates allowed)
/// Special rule: Allows updates even during active terms if course status is PendingApproval
/// </summary>
public class BulkUpdateCourseTopicWeightsCommandHandler : IRequestHandler<BulkUpdateCourseTopicWeightsCommand, BulkUpdateCourseTopicWeightsResponse>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<Assignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;
    private readonly ITopicWeightValidationService _validationService;
    private readonly ITopicWeightHistoryService _historyService;
    private readonly ILogger<BulkUpdateCourseTopicWeightsCommandHandler> _logger;

    public BulkUpdateCourseTopicWeightsCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<Topic> topicRepository,
        IRepository<Course> courseRepository,
        IRepository<Assignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        TopicWeightValidator validator,
        ITopicWeightValidationService validationService,
        ITopicWeightHistoryService historyService,
        ILogger<BulkUpdateCourseTopicWeightsCommandHandler> logger)
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

    public async Task<BulkUpdateCourseTopicWeightsResponse> Handle(BulkUpdateCourseTopicWeightsCommand request, CancellationToken cancellationToken)
    {
        var response = new BulkUpdateCourseTopicWeightsResponse();
        
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
                "Blocked bulk update for course {CourseId}: Assignments exist", 
                request.CourseId);
            return response;
        }

        // VALIDATION 3: Course-aware term validation
        var validation = await _validationService.ValidateForCourseOperationAsync(
            request.CourseId, 
            TopicWeightOperation.Update);
        
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
                "Bulk update allowed for course {CourseId} in PendingApproval status (bypassing term validation)",
                request.CourseId);
        }

        // VALIDATION 4: Get existing weights and validate provided topics
        // Only get course-specific weights (CourseCodeId is NULL for course-specific weights)
        var existingWeights = await _topicWeightRepository.GetManyAsync(
            tw => tw.SpecificCourseId == request.CourseId,
            cancellationToken);
        
        var validationErrors = new List<string>();
        var weightsToProcess = new List<(TopicWeight? Existing, TopicWeightCreateItem Item, Topic Topic)>();
        
        // Validate total weight first
        var totalWeight = request.Weights.Sum(w => w.WeightPercentage);
        if (totalWeight != 100m)
        {
            throw new InvalidOperationException(
                $"Total weight must be exactly 100%. Current total is {totalWeight}%. " +
                "This is a strict requirement for course-specific weights.");
        }
        
        foreach (var item in request.Weights)
        {
            try
            {
                // Validate weight value
                if (item.WeightPercentage < 0 || item.WeightPercentage > 100)
                {
                    validationErrors.Add($"TopicId {item.TopicId}: Weight must be between 0 and 100");
                    continue;
                }

                // Check if topic exists
                var topic = await _topicRepository.GetByIdAsync(item.TopicId, cancellationToken);
                if (topic == null)
                {
                    validationErrors.Add($"TopicId {item.TopicId}: Topic not found");
                    continue;
                }

                // Check if weight already exists for this topic + course
                var existingWeight = existingWeights.FirstOrDefault(tw => tw.TopicId == item.TopicId);
                
                weightsToProcess.Add((existingWeight, item, topic));
            }
            catch (Exception ex)
            {
                validationErrors.Add($"TopicId {item.TopicId}: {ex.Message}");
            }
        }

        // If there are validation errors, return them without making changes
        if (validationErrors.Any() && !weightsToProcess.Any())
        {
            response.Success = false;
            response.Message = "All weights failed validation";
            response.Errors = validationErrors;
            response.FailedCount = request.Weights.Count;
            return response;
        }

        // STEP 1: Handle weights NOT in request - set to 0% (soft delete)
        var topicIdsInRequest = request.Weights.Select(w => w.TopicId).ToHashSet();
        var weightsToSetZero = existingWeights.Where(ew => !topicIdsInRequest.Contains(ew.TopicId)).ToList();
        var setToZeroCount = 0;
        
        foreach (var weight in weightsToSetZero)
        {
            var oldWeight = weight.WeightPercentage;
            weight.WeightPercentage = 0m;
            weight.Description = "Removed from configuration";
            weight.ConfiguredBy = request.ConfiguredBy;
            weight.UpdatedAt = DateTime.UtcNow;
            
            await _topicWeightRepository.UpdateAsync(weight, cancellationToken);
            await _historyService.RecordUpdateAsync(
                weight,
                oldWeight,
                request.ConfiguredBy,
                request.ChangeReason ?? "Set to 0% during bulk update");
            
            setToZeroCount++;
        }
        
        // STEP 2: Process all validated weights (create or update - UPSERT pattern)
        var createdCount = 0;
        var updatedCount = 0;
        
        foreach (var (existingWeight, item, topic) in weightsToProcess)
        {
            try
            {
                TopicWeight topicWeight;
                var oldWeight = existingWeight?.WeightPercentage ?? 0m;
                
                if (existingWeight != null)
                {
                    // UPDATE existing weight
                    existingWeight.WeightPercentage = item.WeightPercentage;
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        existingWeight.Description = item.Description;
                    }
                    existingWeight.ConfiguredBy = request.ConfiguredBy;
                    existingWeight.UpdatedAt = DateTime.UtcNow;
                    
                    // Validate using domain validator
                    var validationResult = _validator.ValidateTopicWeight(existingWeight);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.Add($"TopicId {item.TopicId}: {validationResult.ErrorMessage}");
                        continue;
                    }

                    await _topicWeightRepository.UpdateAsync(existingWeight, cancellationToken);
                    topicWeight = existingWeight;
                    updatedCount++;
                    
                    // Record in history
                    await _historyService.RecordUpdateAsync(
                        topicWeight,
                        oldWeight,
                        request.ConfiguredBy,
                        request.ChangeReason ?? $"Bulk configure for course (Status: {course.Status})");
                }
                else
                {
                    // CREATE new weight
                    topicWeight = new TopicWeight
                    {
                        Id = Guid.NewGuid(),
                        TopicId = item.TopicId,
                        CourseCodeId = null,
                        SpecificCourseId = request.CourseId,
                        WeightPercentage = item.WeightPercentage,
                        Description = item.Description,
                        ConfiguredBy = request.ConfiguredBy,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = null
                    };

                    // Validate using domain validator
                    var validationResult = _validator.ValidateTopicWeight(topicWeight);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.Add($"TopicId {item.TopicId}: {validationResult.ErrorMessage}");
                        continue;
                    }

                    await _topicWeightRepository.AddAsync(topicWeight, cancellationToken);
                    createdCount++;
                    
                    // Record in history
                    await _historyService.RecordCreationAsync(
                        topicWeight,
                        request.ConfiguredBy,
                        request.ChangeReason ?? $"Bulk configure for course (Status: {course.Status})");
                }

                // Use course-aware validation to reflect PendingApproval status correctly
                var weightValidation = await _validationService.ValidateForCourseOperationAsync(
                    request.CourseId, 
                    TopicWeightOperation.Update);

                response.UpdatedWeights.Add(new TopicWeightResponseDto
                {
                    Id = topicWeight.Id,
                    TopicId = topicWeight.TopicId,
                    TopicName = topic.Name,
                    CourseCodeId = null,
                    CourseCodeName = null,
                    SpecificCourseId = topicWeight.SpecificCourseId,
                    SpecificCourseName = course.Name,
                    WeightPercentage = topicWeight.WeightPercentage,
                    Description = topicWeight.Description,
                    ConfiguredBy = topicWeight.ConfiguredBy,
                    ConfiguredAt = topicWeight.CreatedAt,
                    UpdatedAt = topicWeight.UpdatedAt,
                    CanUpdate = weightValidation.IsValid,
                    CanDelete = weightValidation.IsValid,
                    BlockReason = weightValidation.IsValid ? null : weightValidation.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                validationErrors.Add($"TopicId {item.TopicId}: {ex.Message}");
                _logger.LogError(ex, "Error processing TopicWeight for TopicId {TopicId}", item.TopicId);
            }
        }

        if (response.UpdatedWeights.Any())
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            response.Success = true;
            var operations = new List<string>();
            if (createdCount > 0) operations.Add($"{createdCount} created");
            if (updatedCount > 0) operations.Add($"{updatedCount} updated");
            if (setToZeroCount > 0) operations.Add($"{setToZeroCount} set to 0%");
            
            response.Message = $"Successfully processed {response.UpdatedWeights.Count + setToZeroCount} weight(s): {string.Join(", ", operations)}";
            response.SuccessCount = response.UpdatedWeights.Count + setToZeroCount;
            response.FailedCount = validationErrors.Count;
            
            if (validationErrors.Any())
            {
                response.Errors = validationErrors;
            }

            _logger.LogInformation(
                "Bulk configured {Count} TopicWeights for course {CourseId} (Status: {Status}): {Operations}",
                response.UpdatedWeights.Count, request.CourseId, course.Status, string.Join(", ", operations));
        }
        else
        {
            response.Success = false;
            response.Message = "No weights were processed";
            response.Errors = validationErrors;
            response.FailedCount = request.Weights.Count;
        }

        return response;
    }
}
