using MediatR;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Command to create a new topic
/// </summary>
public class CreateTopicCommand : IRequest<CreateTopicResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
