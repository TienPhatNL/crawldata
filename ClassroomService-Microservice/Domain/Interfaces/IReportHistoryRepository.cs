using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for ReportHistory entity
/// Provides methods for tracking and querying report change history
/// </summary>
public interface IReportHistoryRepository : IRepository<ReportHistory>
{
    /// <summary>
    /// Get complete history of changes for a specific report
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of history records ordered by ChangedAt descending</returns>
    Task<List<ReportHistory>> GetReportHistoryAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get specific version of a report from history
    /// Prioritizes content changes (Action = Updated) over status changes
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="version">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>History record for that version or null if not found</returns>
    Task<ReportHistory?> GetVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get specific version and sequence of a report from history
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="version">Version number</param>
    /// <param name="sequence">Sequence number within version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>History record for that version.sequence or null if not found</returns>
    Task<ReportHistory?> GetVersionBySequenceAsync(Guid reportId, int version, int sequence, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all changes made by a specific user within a date range
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="from">Start date (inclusive)</param>
    /// <param name="to">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of changes made by the user</returns>
    Task<List<ReportHistory>> GetUserChangesAsync(string userId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get changes for a report between two versions (inclusive)
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="fromVersion">Starting version</param>
    /// <param name="toVersion">Ending version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of history records between versions</returns>
    Task<List<ReportHistory>> GetVersionRangeAsync(Guid reportId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all sequences for a specific version
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="version">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all sequences ordered by sequence number</returns>
    Task<List<ReportHistory>> GetAllSequencesForVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the last sequence for a specific version
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="version">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last sequence record or null if version not found</returns>
    Task<ReportHistory?> GetLastSequenceForVersionAsync(Guid reportId, int version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get sequences within a range for a specific version
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="version">Version number</param>
    /// <param name="fromSequence">Starting sequence number (inclusive)</param>
    /// <param name="toSequence">Ending sequence number (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sequences in range</returns>
    Task<List<ReportHistory>> GetSequenceRangeAsync(Guid reportId, int version, int fromSequence, int toSequence, CancellationToken cancellationToken = default);
}
