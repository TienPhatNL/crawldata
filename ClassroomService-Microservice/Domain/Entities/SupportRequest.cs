using ClassroomService.Domain.Common;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Entities;

/// <summary>
/// Represents a support request from a lecturer or student to staff within a course context
/// </summary>
public class SupportRequest : BaseAuditableEntity
{
    public Guid CourseId { get; set; }
    
    // Requester Information
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterRole { get; set; } = string.Empty; // Lecturer or Student
    
    // Assigned Staff Information
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    
    // Request Details
    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Pending;
    public SupportPriority Priority { get; set; } = SupportPriority.Medium;
    public SupportRequestCategory Category { get; set; } = SupportRequestCategory.Technical;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON array of image URLs attached to this support request
    /// </summary>
    public string? Images { get; set; }
    
    // Conversation Link (created when accepted)
    public Guid? ConversationId { get; set; }
    
    // Timestamps
    public DateTime RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // Rejection details
    public SupportRequestRejectionReason? RejectionReason { get; set; }
    public string? RejectionComments { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    
    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual Conversation? Conversation { get; set; }
}
