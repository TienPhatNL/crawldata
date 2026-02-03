using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class DeleteTopicWeightCommandHandler : IRequestHandler<DeleteTopicWeightCommand, DeleteTopicWeightResponse>
{
    private readonly IRepository<TopicWeight> _topicWeightRepository;
    private readonly IRepository<Assignment> _assignmentRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITopicWeightValidationService _validationService;
    private readonly ITopicWeightHistoryService _historyService;

    public DeleteTopicWeightCommandHandler(
        IRepository<TopicWeight> topicWeightRepository,
        IRepository<Assignment> assignmentRepository,
        IRepository<Course> courseRepository,
        IUnitOfWork unitOfWork,
        ITopicWeightValidationService validationService,
        ITopicWeightHistoryService historyService)
    {
        _topicWeightRepository = topicWeightRepository;
        _assignmentRepository = assignmentRepository;
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _historyService = historyService;
    }

    public async Task<DeleteTopicWeightResponse> Handle(DeleteTopicWeightCommand request, CancellationToken cancellationToken)
    {
        // STEP 1: Validate term status before allowing deletion
        var validation = await _validationService.ValidateDeleteAsync(request.Id);
        if (!validation.IsValid)
        {
            return new DeleteTopicWeightResponse
            {
                Success = false,
                Message = validation.ErrorMessage ?? "Cannot delete TopicWeight"
            };
        }

        var topicWeight = await _topicWeightRepository.GetByIdAsync(request.Id, cancellationToken);

        if (topicWeight == null)
        {
            return new DeleteTopicWeightResponse
            {
                Success = false,
                Message = $"TopicWeight with ID {request.Id} not found"
            };
        }

        // Check if this topic weight is being used by any assignments
        bool hasAssignments = false;
        
        if (topicWeight.SpecificCourseId.HasValue)
        {
            // Check assignments in the specific course with this topic
            hasAssignments = await _assignmentRepository.ExistsAsync(
                a => a.CourseId == topicWeight.SpecificCourseId.Value && a.TopicId == topicWeight.TopicId,
                cancellationToken);
        }
        else if (topicWeight.CourseCodeId.HasValue)
        {
            // Check assignments in any course with this course code and topic
            var coursesWithCode = await _courseRepository.GetManyAsync(
                c => c.CourseCodeId == topicWeight.CourseCodeId.Value,
                cancellationToken);
            
            var courseIds = coursesWithCode.Select(c => c.Id).ToList();
            
            hasAssignments = await _assignmentRepository.ExistsAsync(
                a => courseIds.Contains(a.CourseId) && a.TopicId == topicWeight.TopicId,
                cancellationToken);
        }

        if (hasAssignments)
        {
            return new DeleteTopicWeightResponse
            {
                Success = false,
                Message = "Cannot delete topic weight because it is assigned to one or more assignments. Please remove or reassign those assignments first."
            };
        }

        // SOFT DELETE: Mark as deleted instead of removing from database
        // This preserves the weight for history and audit trail
        topicWeight.IsDeleted = true;
        topicWeight.DeletedAt = DateTime.UtcNow;
        topicWeight.DeletedBy = request.ConfiguredBy;
        topicWeight.UpdatedAt = DateTime.UtcNow;

        // Record deletion in history BEFORE soft-deleting
        await _historyService.RecordDeletionAsync(
            topicWeight,
            request.ConfiguredBy,
            request.Reason ?? "Deleted by staff");

        await _topicWeightRepository.UpdateAsync(topicWeight, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTopicWeightResponse
        {
            Success = true,
            Message = "TopicWeight soft-deleted successfully (preserved for audit trail)"
        };
    }
}
