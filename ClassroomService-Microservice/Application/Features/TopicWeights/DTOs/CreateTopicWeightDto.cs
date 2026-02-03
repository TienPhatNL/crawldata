namespace ClassroomService.Application.Features.TopicWeights.DTOs;

public class CreateTopicWeightDto
{
    public Guid TopicId { get; set; }
    public Guid? CourseCodeId { get; set; }
    public Guid? SpecificCourseId { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
}
