namespace ClassroomService.Domain.Enums;

/// <summary>
/// Assignment status tracking
/// </summary>
public enum AssignmentStatus
{
    /// <summary>
    /// Assignment is in draft state and not yet published
    /// </summary>
    Draft = 1,
    
    /// <summary>
    /// Assignment is scheduled to become active at StartDate
    /// </summary>
    Scheduled = 2,
    
    /// <summary>
    /// Assignment is currently active and accepting submissions
    /// </summary>
    Active = 3,
    
    /// <summary>
    /// Assignment is past due date but within extended due date (if set)
    /// </summary>
    Extended = 4,
    
    /// <summary>
    /// Assignment is past all due dates
    /// </summary>
    Overdue = 5,
    
    /// <summary>
    /// Assignment has been closed/cancelled by lecturer
    /// </summary>
    Closed = 6
}
