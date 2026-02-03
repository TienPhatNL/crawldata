using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Term-specific operations
/// </summary>
public interface ITermRepository : IRepository<Term>
{
    /// <summary>
    /// Get all active terms
    /// </summary>
    Task<IEnumerable<Term>> GetActiveTermsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get term by name
    /// </summary>
    Task<Term?> GetTermByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a term name exists
    /// </summary>
    Task<bool> TermNameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current active term (based on date range if available)
    /// </summary>
    Task<Term?> GetCurrentTermAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get term with all associated courses
    /// </summary>
    Task<Term?> GetTermWithCoursesAsync(Guid termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overlapping term (excluding the specified termId if provided)
    /// </summary>
    /// <param name="startDate">Start date to check</param>
    /// <param name="endDate">End date to check</param>
    /// <param name="excludeTermId">Term ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overlapping term if found, null otherwise</returns>
    Task<Term?> GetOverlappingTermAsync(DateTime startDate, DateTime endDate, Guid? excludeTermId = null, CancellationToken cancellationToken = default);
}
