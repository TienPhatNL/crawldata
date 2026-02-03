using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.CrawlerChat.Commands;

public class InitiateCrawlFromChatCommand : IRequest<InitiateCrawlFromChatResponse>
{
    public required string MessageContent { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid? AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.CrawlRequest;
    
    /// <summary>
    /// Optional maximum number of pages to crawl (1-500). When provided, overrides prompt-based extraction.
    /// </summary>
    public int? MaxPages { get; set; }
}
