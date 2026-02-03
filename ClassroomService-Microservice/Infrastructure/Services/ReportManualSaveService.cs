using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for handling manual save operations (force flush)
/// Creates a version immediately when user explicitly clicks "Save"
/// </summary>
public class ReportManualSaveService : IReportManualSaveService
{
    private readonly IReportCollaborationBufferService _bufferService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChangeTrackingService _changeTrackingService;
    private readonly ILogger<ReportManualSaveService> _logger;

    public ReportManualSaveService(
        IReportCollaborationBufferService bufferService,
        IUnitOfWork unitOfWork,
        IChangeTrackingService changeTrackingService,
        ILogger<ReportManualSaveService> logger)
    {
        _bufferService = bufferService;
        _unitOfWork = unitOfWork;
        _changeTrackingService = changeTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Force save - create version immediately regardless of debounce period
    /// </summary>
    public async Task<ManualSaveResponse> ForceSaveAsync(Guid reportId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual save requested for report {ReportId} by user {UserId}", reportId, userId);

            // Get buffered changes
            var changes = await _bufferService.GetBufferedChangesAsync(reportId);
            
            if (changes == null || !changes.Any())
            {
                _logger.LogInformation("No pending changes for report {ReportId}, skipping version creation", reportId);
                return new ManualSaveResponse
                {
                    Success = true,
                    Message = "No pending changes to save",
                    NewVersion = 0,
                    SavedAt = DateTime.UtcNow,
                    Contributors = new List<string>()
                };
            }

            // Get session info
            var sessionInfo = await _bufferService.GetSessionInfoAsync(reportId);

            // Get current report
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId, cancellationToken);
            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", reportId);
                return new ManualSaveResponse
                {
                    Success = false,
                    Message = "Report not found",
                    NewVersion = 0,
                    SavedAt = DateTime.UtcNow,
                    Contributors = new List<string>()
                };
            }

            // Calculate metrics
            var firstChange = changes.First();
            var lastChange = changes.Last();
            var editDuration = (lastChange.Timestamp - firstChange.Timestamp).TotalMinutes;
            var charactersAdded = changes.Sum(c => c.ChangeType == "insert" ? c.Content?.Length ?? 0 : 0);
            var charactersDeleted = changes.Sum(c => c.ChangeType == "delete" ? c.Content?.Length ?? 0 : 0);

            // Get the latest content from the last change (full document content)
            var newContent = lastChange.Content;
            var oldContent = report.Submission;

            _logger.LogInformation("💾 Saving content: OLD=[{OldContent}] NEW=[{NewContent}]", 
                oldContent?.Substring(0, Math.Min(100, oldContent?.Length ?? 0)) ?? "null",
                newContent?.Substring(0, Math.Min(100, newContent?.Length ?? 0)) ?? "null");

            // Update the report with the new content
            report.Submission = newContent ?? string.Empty;
            report.Version += 1;
            report.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            
            // CRITICAL: Save changes to database!
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("📝 Manual save: Updated report {ReportId} to version {Version} with new content (length: {Length})", 
                reportId, report.Version, newContent?.Length ?? 0);

            // Generate batch ID
            var batchId = Guid.NewGuid().ToString();

            // Get contributor names for comment
            var contributorNames = sessionInfo?.ActiveUsers.Select(u => u.UserName).ToList() ?? new List<string>();
            var contributorList = contributorNames.Any() ? string.Join(", ", contributorNames) : "Unknown";

            // 🆕 Calculate detailed diff using change tracking service
            var diffResult = _changeTrackingService.CalculateDiff(oldContent, newContent);
            var changeSummary = _changeTrackingService.GenerateSummary(diffResult);
            var changeDetails = _changeTrackingService.SerializeChangeOperations(diffResult);
            var unifiedDiff = _changeTrackingService.CreateUnifiedDiff(oldContent, newContent);

            _logger.LogInformation("📊 Diff calculated: {Summary}", changeSummary);

            // Create version entry (manual save, so IsBatchFlush = false)
            var reportHistory = new ReportHistory
            {
                ReportId = reportId,
                Action = ReportHistoryAction.Updated,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = userId.ToString(),
                Version = report.Version,
                Comment = $"Manual save: {changes.Count} changes from contributors: {contributorList}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { 
                    Submission = oldContent,
                    Status = report.Status.ToString()
                }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { 
                    Submission = newContent,
                    Status = report.Status.ToString()
                }),
                
                // Collaboration-specific fields
                ContributorIds = System.Text.Json.JsonSerializer.Serialize(
                    sessionInfo?.ActiveUsers.Select(u => u.UserId).Distinct().ToList() ?? new List<Guid>()
                ),
                BatchId = Guid.Parse(batchId),
                IsBatchFlush = false, // Manual save, not auto-flush
                CharactersAdded = charactersAdded,
                CharactersDeleted = charactersDeleted,
                EditDuration = TimeSpan.FromMinutes(editDuration),
                
                // 🆕 NEW: Detailed change tracking
                ChangeDetails = changeDetails,
                ChangeSummary = changeSummary,
                UnifiedDiff = unifiedDiff
            };

            // Save history to database
            await _unitOfWork.ReportHistory.AddAsync(reportHistory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clear buffer
            await _bufferService.ClearBufferAsync(reportId);

            _logger.LogInformation(
                "✅ Manual save completed for report {ReportId}: version {VersionId}, " +
                "{ChangeCount} changes, contributors: [{Contributors}], " +
                "{CharsAdded} chars added, {CharsDeleted} chars deleted",
                reportId, reportHistory.Id, changes.Count, contributorList,
                charactersAdded, charactersDeleted);

            return new ManualSaveResponse
            {
                Success = true,
                Message = "Version saved successfully",
                NewVersion = report.Version,
                SavedAt = reportHistory.ChangedAt,
                Contributors = contributorNames
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual save for report {ReportId}", reportId);
            return new ManualSaveResponse
            {
                Success = false,
                Message = $"Error saving version: {ex.Message}",
                NewVersion = 0,
                SavedAt = DateTime.UtcNow,
                Contributors = new List<string>()
            };
        }
    }

    /// <summary>
    /// Check if there are unsaved changes in the buffer
    /// </summary>
    public async Task<bool> HasUnsavedChangesAsync(Guid reportId)
    {
        try
        {
            var changes = await _bufferService.GetBufferedChangesAsync(reportId);
            return changes != null && changes.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking unsaved changes for report {ReportId}", reportId);
            return false;
        }
    }

    /// <summary>
    /// Get count of pending changes
    /// </summary>
    public async Task<int> GetPendingChangeCountAsync(Guid reportId)
    {
        try
        {
            var changes = await _bufferService.GetBufferedChangesAsync(reportId);
            return changes?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending change count for report {ReportId}", reportId);
            return 0;
        }
    }
}
