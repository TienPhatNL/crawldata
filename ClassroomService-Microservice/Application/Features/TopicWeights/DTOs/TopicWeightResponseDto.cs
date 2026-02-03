namespace ClassroomService.Application.Features.TopicWeights.DTOs;

public class TopicWeightResponseDto
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public Guid? CourseCodeId { get; set; }
    public string? CourseCodeName { get; set; }
    public Guid? SpecificCourseId { get; set; }
    public string? SpecificCourseName { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
    public Guid ConfiguredBy { get; set; }
    public DateTime ConfiguredAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Indicates if this weight can be updated (not in active term)
    /// </summary>
    public bool CanUpdate { get; set; }
    
    /// <summary>
    /// Indicates if this weight can be deleted (not in active term)
    /// </summary>
    public bool CanDelete { get; set; }
    
    /// <summary>
    /// Reason why update/delete is blocked (null if allowed)
    /// </summary>
    public string? BlockReason { get; set; }
    
    /// <summary>
    /// Warning message (e.g., total weight < 100%)
    /// </summary>
    public string? Warning { get; set; }
}
