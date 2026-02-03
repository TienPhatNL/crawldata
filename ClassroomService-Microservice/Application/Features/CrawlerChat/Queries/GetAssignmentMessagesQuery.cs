using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CrawlerChat.Queries;

/// <summary>
/// Query to get all crawler chat messages for a specific assignment
/// </summary>
public class GetAssignmentMessagesQuery : IRequest<List<CrawlerChatMessageDto>>
{
    public Guid AssignmentId { get; set; }
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}
