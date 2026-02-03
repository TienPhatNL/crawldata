using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service interface for manual report save operations
/// </summary>
public interface IReportManualSaveService
{
    /// <summary>
    /// Force save - create version immediately regardless of debounce period
    /// </summary>
    Task<ManualSaveResponse> ForceSaveAsync(Guid reportId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if there are unsaved changes in the buffer
    /// </summary>
    Task<bool> HasUnsavedChangesAsync(Guid reportId);

    /// <summary>
    /// Get count of pending changes
    /// </summary>
    Task<int> GetPendingChangeCountAsync(Guid reportId);
}
