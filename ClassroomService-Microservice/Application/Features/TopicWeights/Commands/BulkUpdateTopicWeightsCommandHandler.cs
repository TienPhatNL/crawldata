using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Services;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class BulkUpdateTopicWeightsCommandHandler : IRequestHandler<BulkUpdateTopicWeightsCommand, BulkUpdateTopicWeightsResponse>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<CourseCode> _courseCodeRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TopicWeightValidator _validator;
    private readonly ITopicWeightValidationService _validationService;
    private readonly ITopicWeightHistoryService _historyService;

    public BulkUpdateTopicWeightsCommandHandler(
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

    public async Task<BulkUpdateTopicWeightsResponse> Handle(BulkUpdateTopicWeightsCommand request, CancellationToken cancellationToken)
    {
        var response = new BulkUpdateTopicWeightsResponse();
        
        if (request.Weights == null || !request.Weights.Any())
        {
            response.Success = false;
            response.Message = "No weights provided";
            return response;
        }

        // BUSINESS RULE: Prevent updates if course code has courses in the CURRENT active term
        // Get the current active term first
        var now = DateTime.UtcNow;
        var currentTerms = await _unitOfWork.Terms.GetManyAsync(
            t => t.IsActive && t.StartDate <= now && t.EndDate >= now,
            cancellationToken);
        
        var currentTerm = currentTerms.FirstOrDefault();

        // If there's a current active term, check if this course code has courses in it
        if (currentTerm != null)
        {
            var coursesInCurrentTerm = await _courseRepository.GetManyAsync(
                c => c.CourseCodeId == request.CourseCodeId && c.TermId == currentTerm.Id,
                cancellationToken);

            if (coursesInCurrentTerm.Any())
            {
                response.Success = false;
                response.Message = $"Cannot update topic weights: Course code has {coursesInCurrentTerm.Count()} course(s) in the current active term '{currentTerm.Name}'. " +
                                 "Weight updates are only allowed when there are no courses in the current active term.";
                return response;
            }
        }

        // STEP 1: Validate all weights before making any changes
        var validationErrors = new List<string>();
        var weightsToProcess = new List<(TopicWeight? ExistingWeight, TopicWeightConfigDto Item, Topic Topic)>();
        
        // Get all existing CourseCode-level weights
        var existingWeights = await _topicWeightRepository.GetManyAsync(
            tw => tw.CourseCodeId == request.CourseCodeId && tw.SpecificCourseId == null,
            cancellationToken);
        
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

                // Check if weight already exists for this topic + courseCode
                var existingWeight = existingWeights.FirstOrDefault(tw => tw.TopicId == item.TopicId);
                
                weightsToProcess.Add((existingWeight, item, topic));
            }
            catch (Exception ex)
            {
                validationErrors.Add($"TopicId {item.TopicId}: {ex.Message}");
            }
        }

        // VALIDATION: Calculate total weight for the CourseCode
        if (weightsToProcess.Any())
        {
            var totalWeight = request.Weights.Sum(w => w.WeightPercentage);
            
            if (totalWeight != 100m)
            {
                throw new InvalidOperationException(
                    $"Total weight must be exactly 100%. Current total is {totalWeight}%. " +
                    "This is a strict requirement for all topic weight configurations.");
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

        // STEP 2: Handle weights NOT in request - set to 0% (soft delete)
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
        
        // STEP 3: Create or update all validated weights (UPSERT pattern)
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
                        request.ChangeReason ?? "Bulk configure for CourseCode");
                }
                else
                {
                    // CREATE new weight
                    topicWeight = new TopicWeight
                    {
                        Id = Guid.NewGuid(),
                        TopicId = item.TopicId,
                        CourseCodeId = request.CourseCodeId,
                        SpecificCourseId = null,
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
                        request.ChangeReason ?? "Bulk configure for CourseCode");
                }
                
                // Get related entities for response
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

                // Check validation status for response
                var validation = await _validationService.ValidateUpdateAsync(topicWeight.Id);

                response.UpdatedWeights.Add(new TopicWeightResponseDto
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
                    CanUpdate = validation.IsValid,
                    CanDelete = validation.IsValid,
                    BlockReason = validation.IsValid ? null : validation.ErrorMessage
                });
                
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                validationErrors.Add($"TopicId {item.TopicId}: {ex.Message}");
                response.FailedCount++;
            }
        }

        // Save all changes in a single transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        response.Success = response.SuccessCount > 0;
        var operations = new List<string>();
        if (createdCount > 0) operations.Add($"{createdCount} created");
        if (updatedCount > 0) operations.Add($"{updatedCount} updated");
        if (setToZeroCount > 0) operations.Add($"{setToZeroCount} set to 0%");
        
        response.Message = response.Success
            ? $"Successfully processed {response.SuccessCount + setToZeroCount} weight(s): {string.Join(", ", operations)}"
            : "No topic weights were processed";
        response.Errors = validationErrors;

        return response;
    }
}
