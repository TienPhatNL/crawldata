namespace ClassroomService.Application.Features.TopicWeights.DTOs;

/// <summary>
/// DTO for TopicWeight history record
/// </summary>
public class TopicWeightHistoryDto
{
    public Guid Id { get; set; }
    public Guid TopicWeightId { get; set; }
    public Guid TopicId { get; set; }
    public string? TopicName { get; set; }
    public Guid? CourseCodeId { get; set; }
    public string? CourseCodeName { get; set; }
    public Guid? SpecificCourseId { get; set; }
    public string? SpecificCourseName { get; set; }
    public Guid? TermId { get; set; }
    public string? TermName { get; set; }
    public decimal? OldWeightPercentage { get; set; }
    public decimal NewWeightPercentage { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public string? ChangeReason { get; set; }
    public string? AffectedTerms { get; set; }
}
