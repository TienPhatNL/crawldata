namespace ClassroomService.Domain.Enums;

/// <summary>
/// Report submission status tracking
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Report is saved as draft but not yet submitted
    /// </summary>
    Draft = 1,
    
    /// <summary>
    /// Report has been submitted by student/group leader
    /// </summary>
    Submitted = 2,
    
    /// <summary>
    /// Report is currently under review by instructor
    /// </summary>
    UnderReview = 3,
    
    /// <summary>
    /// Instructor has requested revisions
    /// </summary>
    RequiresRevision = 4,
    
    /// <summary>
    /// Report has been resubmitted after revision request
    /// </summary>
    Resubmitted = 5,
    
    /// <summary>
    /// Report has been graded by instructor
    /// </summary>
    Graded = 6,
    
    /// <summary>
    /// Report was submitted after the due date
    /// </summary>
    Late = 7,
    
    /// <summary>
    /// Report has been rejected by instructor
    /// </summary>
    Rejected = 8
}
