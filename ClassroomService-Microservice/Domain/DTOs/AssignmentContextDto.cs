namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Lightweight DTO for caching assignment context used in crawler prompts
/// </summary>
public class AssignmentContextDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public int? MaxPoints { get; set; }
    public bool IsGroupAssignment { get; set; }
    
    /// <summary>
    /// Course context for better AI understanding
    /// </summary>
    public string CourseName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
}
