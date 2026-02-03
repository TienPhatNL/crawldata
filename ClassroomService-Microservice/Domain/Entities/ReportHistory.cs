using System.ComponentModel.DataAnnotations.Schema;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a historical record of changes made to a report
/// Provides complete audit trail for report lifecycle
/// </summary>
public class ReportHistory : BaseEntity
{
    /// <summary>
    /// Foreign key to the Report being tracked
    /// </summary>
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Navigation property to the Report
    /// </summary>
    public Report Report { get; set; } = null!;
    
    /// <summary>
    /// Type of action performed on the report
    /// </summary>
    public ReportHistoryAction Action { get; set; }
    
    /// <summary>
    /// User ID of the person who made the change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the change occurred (UTC)
    /// </summary>
    public DateTime ChangedAt { get; set; }
    
    /// <summary>
    /// Report version at the time of this change
    /// Increments only for content changes (UpdateReportCommand)
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Sequence number within the version
    /// Increments for all actions within the same version
    /// Example: v2.1 (content update), v2.2 (submission), v2.3 (grading)
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Full version string in format "Version.Sequence"
    /// Example: "2.3" means Version 2, Sequence 3
    /// Not stored in database - computed from Version and SequenceNumber
    /// </summary>
    [NotMapped]
    public string FullVersion => $"{Version}.{SequenceNumber}";
    
    /// <summary>
    /// JSON array of field names that were changed
    /// Example: ["Content", "FilePath", "Status"]
    /// </summary>
    public string? FieldsChanged { get; set; }
    
    /// <summary>
    /// JSON object containing old values before the change
    /// Example: {"Content": "old text", "Grade": null, "Status": "Draft"}
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// JSON object containing new values after the change
    /// Example: {"Content": "new text", "Grade": 85, "Status": "Graded"}
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Optional comment or note about this change
    /// Example: "Edited by team member", "Submitted after deadline"
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// JSON array of user IDs who contributed to this version
    /// Example: ["user-guid-1", "user-guid-2", "user-guid-3"]
    /// Used for collaborative editing sessions
    /// </summary>
    public string? ContributorIds { get; set; }
    
    /// <summary>
    /// Batch ID to group related changes together
    /// All changes made in same editing session share the same BatchId
    /// </summary>
    public Guid? BatchId { get; set; }
    
    /// <summary>
    /// Indicates if this version was created from a debounced batch flush
    /// true = auto-saved after inactivity period
    /// false = manual save or immediate action
    /// </summary>
    public bool IsBatchFlush { get; set; }
    
    /// <summary>
    /// Number of characters added in this change
    /// Used for change analytics and statistics
    /// </summary>
    public int CharactersAdded { get; set; }
    
    /// <summary>
    /// Number of characters deleted in this change
    /// Used for change analytics and statistics
    /// </summary>
    public int CharactersDeleted { get; set; }
    
    /// <summary>
    /// Total duration of the editing session that produced this version
    /// Measured from first change to last change in the batch
    /// </summary>
    public TimeSpan? EditDuration { get; set; }
    
    /// <summary>
    /// Structured JSON array of individual change operations
    /// Format: [
    ///   {"type": "delete", "position": 10, "length": 5, "oldText": "quick"},
    ///   {"type": "insert", "position": 10, "length": 4, "newText": "fast"},
    ///   {"type": "replace", "lineNumber": 3, "oldText": "jumps", "newText": "leaps"}
    /// ]
    /// </summary>
    public string? ChangeDetails { get; set; }
    
    /// <summary>
    /// Human-readable summary of changes
    /// Example: "Modified 3 lines, added 2 paragraphs, deleted 1 sentence"
    /// </summary>
    public string? ChangeSummary { get; set; }
    
    /// <summary>
    /// Unified diff format (like git diff)
    /// Example:
    /// @@ -1,3 +1,3 @@
    /// -The quick brown fox
    /// +The fast brown fox
    /// </summary>
    public string? UnifiedDiff { get; set; }
}
