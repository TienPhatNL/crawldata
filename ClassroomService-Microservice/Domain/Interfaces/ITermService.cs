using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for managing term-related operations and validation
/// </summary>
public interface ITermService
{
    /// <summary>
    /// Check if a term is currently active (within start/end dates)
    /// </summary>
    Task<bool> IsTermActiveAsync(Guid termId);
    
    /// <summary>
    /// Check if a term is past (ended before now)
    /// </summary>
    Task<bool> IsTermPastAsync(Guid termId);
    
    /// <summary>
    /// Get all active terms (now between start and end date)
    /// </summary>
    Task<List<Term>> GetActiveTermsAsync();
    
    /// <summary>
    /// Check if any course using a CourseCode is in an active term
    /// </summary>
    Task<bool> HasActiveTermForCourseCodeAsync(Guid courseCodeId);
    
    /// <summary>
    /// Check if a specific course is in an active term
    /// </summary>
    Task<bool> HasActiveTermForCourseAsync(Guid courseId);
    
    /// <summary>
    /// Check if any course using a CourseCode is in a PAST or ACTIVE term (started already)
    /// </summary>
    Task<bool> HasPastOrActiveTermForCourseCodeAsync(Guid courseCodeId);
    
    /// <summary>
    /// Check if a specific course is in a PAST or ACTIVE term (started already)
    /// </summary>
    Task<bool> HasPastOrActiveTermForCourseAsync(Guid courseId);
    
    /// <summary>
    /// Get affected term names for logging/display (CourseCode or Course level)
    /// </summary>
    Task<List<string>> GetAffectedTermNamesAsync(Guid? courseCodeId, Guid? courseId);
    
    /// <summary>
    /// Get affected term IDs for logging/display (CourseCode or Course level)
    /// </summary>
    Task<List<Guid>> GetAffectedTermIdsAsync(Guid? courseCodeId, Guid? courseId);
    
    /// <summary>
    /// Get primary term for a specific course (for single-term tracking)
    /// </summary>
    Task<Term?> GetCourseTermAsync(Guid courseId);
}
