namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for generating unique course codes
/// </summary>
public interface ICourseUniqueCodeService
{
    /// <summary>
    /// Generates a unique 6-character alphanumeric code for a course
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A unique 6-character code (e.g., "A1B2C3")</returns>
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates if a unique code is already in use
    /// </summary>
    /// <param name="uniqueCode">The code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code is available, false if already used</returns>
    Task<bool> IsCodeAvailableAsync(string uniqueCode, CancellationToken cancellationToken = default);
}
