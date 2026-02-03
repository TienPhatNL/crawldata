using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.CrawlerChat.Queries;

/// <summary>
/// Query to get all crawler chat messages for a specific conversation
/// </summary>
public class GetConversationMessagesQuery : IRequest<List<CrawlerChatMessageDto>>
{
    public Guid ConversationId { get; set; }
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}
