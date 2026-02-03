using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service interface for managing report collaboration buffer in Redis
/// Handles temporary storage of changes before they are persisted to database
/// </summary>
public interface IReportCollaborationBufferService
{
    /// <summary>
    /// Add a change to the report buffer
    /// </summary>
    Task AddChangeAsync(ReportChangeDto change, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all buffered changes for a report
    /// </summary>
    Task<List<ReportChangeDto>> GetBufferedChangesAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get count of pending changes for a report
    /// </summary>
    Task<int> GetPendingChangesCountAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all buffered changes for a report
    /// </summary>
    Task ClearBufferAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add a user to the active collaboration session
    /// </summary>
    Task AddUserToSessionAsync(Guid reportId, Guid userId, string userName, string userEmail, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a user from the active collaboration session
    /// </summary>
    Task RemoveUserFromSessionAsync(Guid reportId, Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all active users in a collaboration session
    /// </summary>
    Task<List<CollaboratorPresenceDto>> GetActiveUsersAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all contributors who made changes to a report (from buffer)
    /// </summary>
    Task<List<Guid>> GetContributorsAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update last activity timestamp for a report
    /// </summary>
    Task UpdateLastActivityAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get time elapsed since last activity on a report
    /// </summary>
    Task<TimeSpan> GetInactivityDurationAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get last activity timestamp for a report
    /// </summary>
    Task<DateTime?> GetLastActivityAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all report IDs that have pending changes
    /// </summary>
    Task<List<Guid>> GetReportsWithPendingChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all active collaboration session report IDs
    /// </summary>
    Task<List<Guid>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if buffer should be flushed based on size or time constraints
    /// </summary>
    Task<bool> ShouldFlushBufferAsync(Guid reportId, int debounceSeconds = 60, int maxBufferSize = 200, int maxBufferMinutes = 5, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get session information for a report
    /// </summary>
    Task<ReportCollaborationSessionDto?> GetSessionInfoAsync(Guid reportId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the latest working content (from buffer if available, null if no buffer)
    /// </summary>
    Task<string?> GetLatestWorkingContentAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store a pending change that hasn't been broadcasted yet (for server-side debouncing)
    /// </summary>
    Task SetPendingChangeAsync(Guid reportId, ReportChangeDto change, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the pending change for a report (for server-side debouncing)
    /// </summary>
    Task<ReportChangeDto?> GetPendingChangeAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the pending change for a report (for server-side debouncing)
    /// </summary>
    Task ClearPendingChangeAsync(Guid reportId, CancellationToken cancellationToken = default);
}
