using ClassroomService.Domain.DTOs;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetConversationMessagesQuery : IRequest<GetConversationMessagesResponse>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Optional: Filter messages by specific support request
    /// When NULL, returns all messages (general chat behavior)
    /// </summary>
    public Guid? SupportRequestId { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetConversationMessagesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    
    /// <summary>
    /// Indicates if the chat is read-only (cannot send new messages)
    /// True when support request is not InProgress, or conversation is closed
    /// </summary>
    public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Human-readable reason why the chat is read-only
    /// </summary>
    public string? ReadOnlyReason { get; set; }
}
