using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Lightweight DTO for listing support requests
/// </summary>
public class SupportRequestListDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterRole { get; set; } = string.Empty;
    
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    
    public SupportRequestStatus Status { get; set; }
    public SupportPriority Priority { get; set; }
    public SupportRequestCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON array of image URLs attached to this support request
    /// </summary>
    public string? Images { get; set; }
    
    public DateTime RequestedAt { get; set; }
}
