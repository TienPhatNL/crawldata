using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Queries;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, GetConversationMessagesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;

    public GetConversationMessagesQueryHandler(IUnitOfWork unitOfWork, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<GetConversationMessagesResponse> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate access to conversation
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                return new GetConversationMessagesResponse
                {
                    Success = false,
                    Message = "Conversation not found",
                    Messages = new List<ChatMessageDto>()
                };
            }

            if (conversation.User1Id != request.UserId && conversation.User2Id != request.UserId)
            {
                return new GetConversationMessagesResponse
                {
                    Success = false,
                    Message = "Access denied to this conversation",
                    Messages = new List<ChatMessageDto>()
                };
            }

            // Get messages, filtering by SupportRequestId if provided (filtering happens at DB level before pagination)
            var messages = await _unitOfWork.Chats
                .GetConversationMessagesAsync(
                    request.ConversationId, 
                    request.PageNumber, 
                    request.PageSize, 
                    request.SupportRequestId);

            // Check if chat should be read-only (for support requests)
            bool isReadOnly = false;
            string? readOnlyReason = null;
            
            if (request.SupportRequestId.HasValue)
            {
                var supportRequest = await _unitOfWork.SupportRequests
                    .GetByIdAsync(request.SupportRequestId.Value, cancellationToken);
                
                if (supportRequest != null && supportRequest.Status != Domain.Enums.SupportRequestStatus.InProgress)
                {
                    isReadOnly = true;
                    readOnlyReason = supportRequest.Status switch
                    {
                        Domain.Enums.SupportRequestStatus.Pending => "⏳ Support request is pending staff acceptance",
                        Domain.Enums.SupportRequestStatus.Resolved => "✅ Support request has been resolved",
                        Domain.Enums.SupportRequestStatus.Cancelled => "❌ Support request was cancelled",
                        Domain.Enums.SupportRequestStatus.Rejected => "🚫 Support request was rejected by staff",
                        _ => "Support request is not active"
                    };
                }
            }
            // Check if conversation is closed (general chat or support request)
            else if (conversation.IsClosed)
            {
                isReadOnly = true;
                readOnlyReason = "This conversation has been closed";
            }

            var result = new List<ChatMessageDto>();

            foreach (var msg in messages)
            {
                var sender = await _userService.GetUserByIdAsync(msg.SenderId);
                var receiver = await _userService.GetUserByIdAsync(msg.ReceiverId);

                result.Add(new ChatMessageDto
                {
                    Id = msg.Id,
                    SenderId = msg.SenderId,
                    SenderName = sender?.FullName ?? "Unknown",
                    ReceiverId = msg.ReceiverId,
                    ReceiverName = receiver?.FullName ?? "Unknown",
                    Message = msg.IsDeleted ? "" : msg.Message,
                    SentAt = msg.SentAt,
                    IsDeleted = msg.IsDeleted,
                    IsRead = msg.IsRead,
                    ReadAt = msg.ReadAt
                });
            }

            // Reverse to show oldest first (since we query newest first for pagination)
            result.Reverse();

            return new GetConversationMessagesResponse
            {
                Success = true,
                Message = "Messages retrieved successfully",
                Messages = result,
                IsReadOnly = isReadOnly,
                ReadOnlyReason = readOnlyReason
            };
        }
        catch (Exception ex)
        {
            return new GetConversationMessagesResponse
            {
                Success = false,
                Message = $"Error retrieving messages: {ex.Message}",
                Messages = new List<ChatMessageDto>()
            };
        }
    }
}
