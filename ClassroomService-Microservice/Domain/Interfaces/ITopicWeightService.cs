namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for resolving topic weights for assignments
/// </summary>
public interface ITopicWeightService
{
    Task<decimal> ResolveTopicWeightAsync(Guid topicId, Guid courseCodeId, Guid courseId, bool divideByAssignmentCount = true);
    Task<decimal> CalculateWeightedGradeAsync(Guid courseId, Guid studentId);
}
