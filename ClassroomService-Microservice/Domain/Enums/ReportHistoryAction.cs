namespace ClassroomService.Domain.Enums;

/// <summary>
/// Represents the type of action performed on a report
/// </summary>
public enum ReportHistoryAction
{
    /// <summary>
    /// Initial report draft creation
    /// </summary>
    Created = 0,
    
    /// <summary>
    /// Report content or file updated (collaborative editing)
    /// </summary>
    Updated = 1,
    
    /// <summary>
    /// Draft submitted for review (Draft → Submitted/Late)
    /// </summary>
    Submitted = 2,
    
    /// <summary>
    /// Report resubmitted after revision (RequiresRevision → Resubmitted)
    /// </summary>
    Resubmitted = 3,
    
    /// <summary>
    /// Report graded by lecturer
    /// </summary>
    Graded = 4,
    
    /// <summary>
    /// Lecturer requested revisions (→ RequiresRevision)
    /// </summary>
    RevisionRequested = 5,
    
    /// <summary>
    /// Report rejected by lecturer (FINAL rejection, not requiring revision)
    /// </summary>
    Rejected = 6,
    
    /// <summary>
    /// Any other status change not covered by specific actions
    /// </summary>
    StatusChanged = 7,
    
    /// <summary>
    /// Student/leader reverted submitted report back to draft (Submitted → Draft)
    /// </summary>
    RevertedToDraft = 8,
    
    /// <summary>
    /// Report content restored to a previous historical version
    /// </summary>
    ContentReverted = 9
}
