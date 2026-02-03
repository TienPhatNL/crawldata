namespace ClassroomService.Domain.Enums;

/// <summary>
/// Course approval status
/// </summary>
public enum CourseStatus
{
    /// <summary>
    /// Course awaiting staff approval
    /// </summary>
    PendingApproval = 1,
    
    /// <summary>
    /// Course approved and visible to students
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// Course deactivated
    /// </summary>
    Inactive = 3,
    
    /// <summary>
    /// Course rejected by staff
    /// </summary>
    Rejected = 4
}
