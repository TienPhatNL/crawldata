using MediatR;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Command to delete a topic
/// </summary>
public class DeleteTopicCommand : IRequest<DeleteTopicResponse>
{
    public Guid Id { get; set; }
}
