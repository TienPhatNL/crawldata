namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for validating TopicWeight operations based on term status
/// </summary>
public interface ITopicWeightValidationService
{
    /// <summary>
    /// Validate if a TopicWeight can be updated
    /// </summary>
    Task<TopicWeightValidationResult> ValidateUpdateAsync(Guid topicWeightId);
    
    /// <summary>
    /// Validate if a TopicWeight can be deleted
    /// </summary>
    Task<TopicWeightValidationResult> ValidateDeleteAsync(Guid topicWeightId);
    
    /// <summary>
    /// Validate if a new TopicWeight can be created (warnings only)
    /// </summary>
    Task<TopicWeightValidationResult> ValidateCreateAsync(Guid? courseCodeId, Guid? specificCourseId);
    
    /// <summary>
    /// Validate if a TopicWeight operation can be performed for a specific course
    /// Special rule: Bypasses term validation if course status is PendingApproval
    /// </summary>
    Task<TopicWeightValidationResult> ValidateForCourseOperationAsync(Guid courseId, TopicWeightOperation operation);
}

/// <summary>
/// TopicWeight operation types for validation
/// </summary>
public enum TopicWeightOperation
{
    Create,
    Update,
    Delete
}

/// <summary>
/// Result of TopicWeight validation
/// </summary>
public class TopicWeightValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> AffectedTerms { get; set; } = new();
    public ValidationLevel Level { get; set; }
    
    public static TopicWeightValidationResult Success() 
        => new() { IsValid = true, Level = ValidationLevel.Success };
    
    public static TopicWeightValidationResult Forbidden(string message, List<string> terms) 
        => new() { IsValid = false, ErrorMessage = message, AffectedTerms = terms, Level = ValidationLevel.Error };
    
    public static TopicWeightValidationResult Warning(string message, List<string> terms) 
        => new() { IsValid = true, ErrorMessage = message, AffectedTerms = terms, Level = ValidationLevel.Warning };
}

/// <summary>
/// Validation severity level
/// </summary>
public enum ValidationLevel
{
    Success,
    Warning,
    Error
}
