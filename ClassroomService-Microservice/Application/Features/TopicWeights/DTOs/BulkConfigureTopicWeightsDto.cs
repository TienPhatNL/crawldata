namespace ClassroomService.Application.Features.TopicWeights.DTOs;

public class BulkConfigureTopicWeightsDto
{
    public Guid CourseCodeId { get; set; }
    public List<TopicWeightConfigDto> TopicWeights { get; set; } = new();
}

public class TopicWeightConfigDto
{
    public Guid TopicId { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
}
