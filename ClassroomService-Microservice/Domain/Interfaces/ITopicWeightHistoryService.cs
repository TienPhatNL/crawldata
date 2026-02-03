using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for tracking TopicWeight changes in history
/// </summary>
public interface ITopicWeightHistoryService
{
    /// <summary>
    /// Record creation of a new TopicWeight
    /// </summary>
    Task RecordCreationAsync(TopicWeight topicWeight, Guid userId, string? reason = null);
    
    /// <summary>
    /// Record update of an existing TopicWeight
    /// </summary>
    Task RecordUpdateAsync(TopicWeight topicWeight, decimal oldWeight, Guid userId, string? reason = null);
    
    /// <summary>
    /// Record deletion of a TopicWeight
    /// </summary>
    Task RecordDeletionAsync(TopicWeight topicWeight, Guid userId, string? reason = null);
    
    /// <summary>
    /// Get history for a specific TopicWeight
    /// </summary>
    Task<List<TopicWeightHistory>> GetHistoryAsync(Guid topicWeightId);
    
    /// <summary>
    /// Get all history records (for auditing)
    /// </summary>
    Task<List<TopicWeightHistory>> GetAllHistoryAsync();
}
