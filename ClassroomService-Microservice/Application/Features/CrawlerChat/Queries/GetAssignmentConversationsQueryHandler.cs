using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.CrawlerChat.Queries;

public class GetAssignmentConversationsQueryHandler
    : IRequestHandler<GetAssignmentConversationsQuery, List<ConversationSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAssignmentConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ConversationSummaryDto>> Handle(
        GetAssignmentConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var messages = await _unitOfWork.CrawlerChatMessages.GetByAssignmentIdAsync(request.AssignmentId);

        // Get unique conversation IDs
        var conversationIds = messages.Select(m => m.ConversationId).Distinct().ToList();

        // Fetch conversation names from Conversations table
        var conversationNames = await _unitOfWork.Conversations
            .GetAllAsync(cancellationToken);
        var conversationNameDict = conversationNames
            .Where(c => conversationIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => c.Name);

        // Group by ConversationId and create summaries
        var conversations = messages
            .GroupBy(m => m.ConversationId)
            .Select(g => new ConversationSummaryDto
            {
                ConversationId = g.Key,
                ConversationName = conversationNameDict.TryGetValue(g.Key, out var name) ? name : null,
                MessageCount = g.Count(),
                LastMessageAt = g.Max(m => m.CreatedAt),
                ParticipantNames = new List<string>() // Will be populated from user service if needed
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();

        // Optional: filter by user if specified
        if (request.UserId.HasValue)
        {
            var userConversationIds = messages
                .Where(m => m.SenderId == request.UserId.Value)
                .Select(m => m.ConversationId)
                .Distinct()
                .ToHashSet();

            conversations = conversations
                .Where(c => userConversationIds.Contains(c.ConversationId))
                .ToList();
        }

        return conversations;
    }
}
