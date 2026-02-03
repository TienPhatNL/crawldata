using MediatR;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

public class DeleteTopicWeightCommand : IRequest<DeleteTopicWeightResponse>
{
    public Guid Id { get; set; }
    public Guid ConfiguredBy { get; set; } // User performing the deletion
    public string? Reason { get; set; } // Optional reason for deletion
}

public class DeleteTopicWeightResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
