using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class CreateTopicWeightCommand : IRequest<TopicWeightResponseDto>
{
    public Guid TopicId { get; set; }
    public Guid? CourseCodeId { get; set; }
    public Guid? SpecificCourseId { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
    public Guid ConfiguredBy { get; set; } // From authenticated user
    public string? ChangeReason { get; set; } // Optional reason for creating this weight
}
