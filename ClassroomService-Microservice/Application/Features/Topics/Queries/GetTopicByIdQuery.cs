using MediatR;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Query to get a topic by ID
/// </summary>
public class GetTopicByIdQuery : IRequest<GetTopicByIdResponse>
{
    public Guid Id { get; set; }
}
