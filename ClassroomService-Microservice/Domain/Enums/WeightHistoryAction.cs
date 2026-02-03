namespace ClassroomService.Domain.Enums;

/// <summary>
/// Represents the type of action performed on a TopicWeight
/// </summary>
public enum WeightHistoryAction
{
    /// <summary>
    /// TopicWeight was created
    /// </summary>
    Created = 0,
    
    /// <summary>
    /// TopicWeight was updated
    /// </summary>
    Updated = 1,
    
    /// <summary>
    /// TopicWeight was deleted
    /// </summary>
    Deleted = 2
}
