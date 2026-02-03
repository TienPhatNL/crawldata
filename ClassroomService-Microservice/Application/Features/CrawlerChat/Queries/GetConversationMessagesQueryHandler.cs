using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CrawlerChat.Queries;

/// <summary>
/// Handler for retrieving crawler chat messages for a specific conversation
/// </summary>
public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, List<CrawlerChatMessageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetConversationMessagesQueryHandler> _logger;

    public GetConversationMessagesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetConversationMessagesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<CrawlerChatMessageDto>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving messages for conversation {ConversationId}", request.ConversationId);

            // Get messages from repository
            var messages = await _unitOfWork.CrawlerChatMessages.GetByConversationIdAsync(request.ConversationId, cancellationToken);

            // Apply pagination
            var paginatedMessages = messages
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToList();

            // Map to DTOs
            var messageDtos = paginatedMessages.Select(m => new CrawlerChatMessageDto
            {
                MessageId = m.Id,
                ConversationId = m.ConversationId,
                UserId = m.SenderId,
                UserName = (m.IsSystemMessage || m.SenderId == Guid.Empty) ? "Crawler Agent" : "User",
                Content = m.MessageContent,
                GroupId = m.GroupId,
                AssignmentId = m.AssignmentId,
                MessageType = m.MessageType,
                CrawlJobId = m.CrawlJobId,
                Timestamp = m.CreatedAt,
                ExtractedData = m.CrawlResultSummary,
                VisualizationData = m.MetadataJson
            }).ToList();

            _logger.LogInformation("Retrieved {Count} messages for conversation {ConversationId}",
                messageDtos.Count, request.ConversationId);

            return messageDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", request.ConversationId);
            throw;
        }
    }
}
