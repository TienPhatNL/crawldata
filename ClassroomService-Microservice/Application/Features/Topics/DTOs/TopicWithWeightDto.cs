namespace ClassroomService.Application.Features.Topics.DTOs;

public class TopicWithWeightDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
}
