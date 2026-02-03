using System.Text.Json;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for tracking changes to reports
/// Automatically logs all report modifications to ReportHistory table
/// </summary>
public class ReportHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportHistoryService> _logger;

    public ReportHistoryService(IUnitOfWork unitOfWork, ILogger<ReportHistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Track a change made to a report
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="action">Type of action performed</param>
    /// <param name="changedBy">User ID who made the change</param>
    /// <param name="version">Current version of the report</param>
    /// <param name="oldValues">Dictionary of old values (field name -> value)</param>
    /// <param name="newValues">Dictionary of new values (field name -> value)</param>
    /// <param name="comment">Optional comment about the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task TrackChangeAsync(
        Guid reportId,
        ReportHistoryAction action,
        string changedBy,
        int version,
        Dictionary<string, object>? oldValues = null,
        Dictionary<string, object>? newValues = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        // Calculate next sequence number for this version
        var existingRecords = await _unitOfWork.ReportHistory.GetManyAsync(
            h => h.ReportId == reportId && h.Version == version, 
            cancellationToken);
        var nextSequence = existingRecords.Any() 
            ? existingRecords.Max(h => h.SequenceNumber) + 1 
            : 1;

        var history = new ReportHistory
        {
            ReportId = reportId,
            Action = action,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Version = version,
            SequenceNumber = nextSequence,
            Comment = comment
        };

        // Serialize field changes if provided
        if (oldValues != null && oldValues.Count > 0)
        {
            _logger.LogInformation("[TrackChangeAsync DEBUG] About to serialize oldValues (Count={Count}): {OldValues}", oldValues.Count, JsonSerializer.Serialize(oldValues));
            history.FieldsChanged = JsonSerializer.Serialize(oldValues.Keys);
            history.OldValues = JsonSerializer.Serialize(oldValues);
            _logger.LogInformation("[TrackChangeAsync DEBUG] Serialized OldValues: {SerializedOldValues}", history.OldValues);
        }

        if (newValues != null && newValues.Count > 0)
        {
            _logger.LogInformation("[TrackChangeAsync DEBUG] About to serialize newValues (Count={Count}): {NewValues}", newValues.Count, JsonSerializer.Serialize(newValues));
            if (history.FieldsChanged == null && oldValues == null)
            {
                // If only new values provided, track just those fields
                history.FieldsChanged = JsonSerializer.Serialize(newValues.Keys);
            }
            history.NewValues = JsonSerializer.Serialize(newValues);
            _logger.LogInformation("[TrackChangeAsync DEBUG] Serialized NewValues: {SerializedNewValues}", history.NewValues);
        }

        await _unitOfWork.ReportHistory.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Track report creation
    /// </summary>
    public async Task TrackCreationAsync(
        Guid reportId,
        string createdBy,
        string? content,
        string? filePath,
        CancellationToken cancellationToken = default)
    {
        var newValues = new Dictionary<string, object>
        {
            ["Submission"] = content ?? "",  // Use "Submission" not "Content"
            ["FilePath"] = filePath ?? "",
            ["Status"] = "Draft"
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Created,
            createdBy,
            version: 1,
            newValues: newValues,
            comment: "Initial draft created",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Track report update (collaborative editing)
    /// </summary>
    public async Task TrackUpdateAsync(
        Guid reportId,
        string updatedBy,
        int newVersion,
        string? oldContent,
        string? newContent,
        string? oldFilePath,
        string? newFilePath,
        string? status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TrackUpdateAsync DEBUG] Status parameter: '{Status}'", status ?? "null");
        _logger.LogInformation("[TrackUpdateAsync DEBUG] oldFilePath: '{OldFilePath}', newFilePath: '{NewFilePath}'", oldFilePath ?? "null", newFilePath ?? "null");
        
        var oldValues = new Dictionary<string, object>
        {
            ["Submission"] = oldContent ?? "",  // Use "Submission" not "Content"
            ["FilePath"] = oldFilePath ?? "",
            ["Status"] = status ?? ""
        };

        var newValues = new Dictionary<string, object>
        {
            ["Submission"] = newContent ?? "",  // Use "Submission" not "Content"
            ["FilePath"] = newFilePath ?? "",
            ["Status"] = status ?? ""
        };
        
        _logger.LogInformation("[TrackUpdateAsync DEBUG] oldValues: {OldValues}", System.Text.Json.JsonSerializer.Serialize(oldValues));
        _logger.LogInformation("[TrackUpdateAsync DEBUG] newValues: {NewValues}", System.Text.Json.JsonSerializer.Serialize(newValues));

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Updated,
            updatedBy,
            newVersion,
            oldValues,
            newValues,
            $"Edited by user",
            cancellationToken);
    }

    /// <summary>
    /// Track draft submission
    /// </summary>
    public async Task TrackSubmissionAsync(
        Guid reportId,
        string submittedBy,
        int version,
        string newStatus,
        DateTime submittedAt,
        bool isLate,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>
        {
            ["Status"] = "Draft"
        };

        var newValues = new Dictionary<string, object>
        {
            ["Status"] = newStatus,
            ["SubmittedAt"] = submittedAt.ToString("O")
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Submitted,
            submittedBy,
            version,
            oldValues,
            newValues,
            isLate ? "Submitted after deadline" : "Submitted on time",
            cancellationToken);
    }

    /// <summary>
    /// Track report grading
    /// </summary>
    public async Task TrackGradingAsync(
        Guid reportId,
        string gradedBy,
        int version,
        decimal? oldGrade,
        decimal newGrade,
        string? oldFeedback,
        string? newFeedback,
        string oldStatus,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>
        {
            ["Grade"] = oldGrade?.ToString() ?? "null",
            ["Feedback"] = oldFeedback ?? "",
            ["Status"] = oldStatus
        };

        var newValues = new Dictionary<string, object>
        {
            ["Grade"] = newGrade.ToString(),
            ["Feedback"] = newFeedback ?? "",
            ["Status"] = "Graded"
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Graded,
            gradedBy,
            version,
            oldValues,
            newValues,
            "Graded by lecturer",
            cancellationToken);
    }

    /// <summary>
    /// Track resubmission after revision
    /// </summary>
    public async Task TrackResubmissionAsync(
        Guid reportId,
        string resubmittedBy,
        int newVersion,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>
        {
            ["Status"] = "RequiresRevision"
        };

        var newValues = new Dictionary<string, object>
        {
            ["Status"] = "Resubmitted",
            ["Version"] = newVersion.ToString()
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Resubmitted,
            resubmittedBy,
            newVersion,
            oldValues,
            newValues,
            "Resubmitted after revision",
            cancellationToken);
    }

    /// <summary>
    /// Track revision request
    /// </summary>
    public async Task TrackRevisionRequestAsync(
        Guid reportId,
        string requestedBy,
        int version,
        string feedback,
        string oldStatus,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>
        {
            ["Status"] = oldStatus
        };

        var newValues = new Dictionary<string, object>
        {
            ["Status"] = "RequiresRevision",
            ["Feedback"] = feedback
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.RevisionRequested,
            requestedBy,
            version,
            oldValues,
            newValues,
            "Revision requested by lecturer",
            cancellationToken);
    }

    /// <summary>
    /// Track report rejection (FINAL rejection, not requiring revision)
    /// </summary>
    public async Task TrackRejectionAsync(
        Guid reportId,
        string rejectedBy,
        int version,
        string feedback,
        string oldStatus,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>
        {
            ["Status"] = oldStatus
        };

        var newValues = new Dictionary<string, object>
        {
            ["Status"] = "Rejected",
            ["Feedback"] = feedback
        };

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Rejected,
            rejectedBy,
            version,
            oldValues,
            newValues,
            "Report rejected by lecturer (final rejection)",
            cancellationToken);
    }
    
    /// <summary>
    /// Track file upload/change
    /// Does not delete old files - maintains versioning history
    /// </summary>
    public async Task TrackFileUploadAsync(
        Guid reportId,
        string uploadedBy,
        int version,
        string? oldFileUrl,
        string? newFileUrl,
        CancellationToken cancellationToken = default)
    {
        var oldValues = new Dictionary<string, object>();
        var newValues = new Dictionary<string, object>();
        var comment = "";

        if (string.IsNullOrEmpty(oldFileUrl))
        {
            // First time upload
            newValues["FileUrl"] = newFileUrl ?? "";
            comment = "File attached to report";
        }
        else
        {
            // File replacement - keep old version for history
            oldValues["FileUrl"] = oldFileUrl;
            newValues["FileUrl"] = newFileUrl ?? "";
            comment = "File updated (previous version preserved)";
        }

        await TrackChangeAsync(
            reportId,
            ReportHistoryAction.Updated,
            uploadedBy,
            version,
            oldValues,
            newValues,
            comment,
            cancellationToken);
    }
}
