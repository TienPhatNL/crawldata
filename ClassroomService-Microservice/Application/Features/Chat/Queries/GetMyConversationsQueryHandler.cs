using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetMyConversationsQueryHandler : IRequestHandler<GetMyConversationsQuery, GetMyConversationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetMyConversationsQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetMyConversationsResponse> Handle(GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var conversations = await _unitOfWork.Conversations
                .GetUserConversationsAsync(request.UserId, request.CourseId);

            // Get all unread counts in a single query for performance
            var unreadCounts = await _unitOfWork.Chats
                .GetUnreadCountsAsync(request.UserId, cancellationToken);

            var result = new List<ConversationDto>();

            foreach (var conv in conversations)
            {
                // Determine the other user in the conversation
                var otherUserId = conv.User1Id == request.UserId ? conv.User2Id : conv.User1Id;
                var otherUser = await _userService.GetUserByIdAsync(otherUserId);

                // Get unread count for this conversation
                unreadCounts.TryGetValue(conv.Id, out int unreadCount);

                result.Add(new ConversationDto
                {
                    Id = conv.Id,
                    CourseId = conv.CourseId,
                    CourseName = conv.Course?.Name ?? "Unknown Course",
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser?.FullName ?? "Unknown User",
                    OtherUserRole = otherUser?.Role ?? "Unknown",
                    LastMessagePreview = conv.LastMessagePreview,
                    LastMessageAt = conv.LastMessageAt,
                    UnreadCount = unreadCount
                });
            }

            return new GetMyConversationsResponse
            {
                Success = true,
                Message = "Conversations retrieved successfully",
                Conversations = result
            };
        }
        catch (Exception ex)
        {
            return new GetMyConversationsResponse
            {
                Success = false,
                Message = $"Error retrieving conversations: {ex.Message}",
                Conversations = new List<ConversationDto>()
            };
        }
    }
}
