using System.Collections.Concurrent;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClassroomService.Application.Hubs;

/// <summary>
/// SignalR Hub for real-time peer-to-peer chat
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<ChatHub> _logger;

    // Track connections: UserId -> List<ConnectionId>
    private static readonly ConcurrentDictionary<Guid, List<string>> _connections = new();

    // Track staff connections in support room
    private static readonly ConcurrentDictionary<string, Guid> _supportStaffConnections = new();

    public ChatHub(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<ChatHub> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    // ==================== CONNECTION MANAGEMENT ====================

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            Context.Abort();
            return;
        }

        _connections.AddOrUpdate(
            userId.Value,
            new List<string> { Context.ConnectionId },
            (_, list) =>
            {
                list.Add(Context.ConnectionId);
                return list;
            });

        _logger.LogInformation("User {UserId} connected to chat hub", userId.Value);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue && _connections.TryGetValue(userId.Value, out var list))
        {
            list.Remove(Context.ConnectionId);
            if (list.Count == 0)
            {
                _connections.TryRemove(userId.Value, out _);
            }
        }
        _logger.LogInformation("User {UserId} disconnected from chat hub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    // ==================== SEND MESSAGE ====================

    /// <summary>
    /// Send a text message to another user
    /// </summary>
    public async Task SendMessage(SendMessageDto dto)
    {
        var senderId = _currentUserService.UserId!.Value;

        // Validate message
        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            await Clients.Caller.SendAsync("Error", "Message cannot be empty");
            return;
        }

        if (dto.Message.Length > 2000)
        {
            await Clients.Caller.SendAsync("Error", "Message too long (max 2000 chars)");
            return;
        }

        // Prevent users from messaging themselves
        if (senderId == dto.ReceiverId)
        {
            await Clients.Caller.SendAsync("Error", "You cannot send messages to yourself");
            return;
        }

        // Validate course access for both users
        if (!await HasCourseAccess(senderId, dto.CourseId))
        {
            await Clients.Caller.SendAsync("Error", "You don't have access to this course");
            return;
        }

        if (!await HasCourseAccess(dto.ReceiverId, dto.CourseId))
        {
            await Clients.Caller.SendAsync("Error", "Receiver doesn't have access to this course");
            return;
        }

        try
        {
            // Get or create conversation
            var conversation = await _unitOfWork.Conversations
                .GetConversationAsync(dto.CourseId, senderId, dto.ReceiverId);

            // Check if conversation is closed
            if (conversation != null && conversation.IsClosed)
            {
                await Clients.Caller.SendAsync("Error", "This conversation has been closed and cannot accept new messages");
                return;
            }

            // Validate support request status if SupportRequestId is provided
            if (dto.SupportRequestId.HasValue)
            {
                var supportRequest = await _unitOfWork.SupportRequests
                    .GetByIdAsync(dto.SupportRequestId.Value);

                if (supportRequest == null)
                {
                    await Clients.Caller.SendAsync("Error", "Support request not found");
                    return;
                }

                if (supportRequest.Status != Domain.Enums.SupportRequestStatus.InProgress)
                {
                    var statusMessage = supportRequest.Status switch
                    {
                        Domain.Enums.SupportRequestStatus.Pending => "Support request is pending acceptance",
                        Domain.Enums.SupportRequestStatus.Resolved => "Support request has been resolved",
                        Domain.Enums.SupportRequestStatus.Cancelled => "Support request was cancelled",
                        Domain.Enums.SupportRequestStatus.Rejected => "Support request was rejected",
                        _ => "Support request is not active"
                    };

                    await Clients.Caller.SendAsync("Error", $"Cannot send message: {statusMessage}");
                    _logger.LogWarning("User {UserId} attempted to send message to inactive support request {SupportRequestId} with status {Status}",
                        senderId, dto.SupportRequestId.Value, supportRequest.Status);
                    return;
                }

                // Verify user is part of this support request
                if (senderId != supportRequest.RequesterId && senderId != supportRequest.AssignedStaffId)
                {
                    await Clients.Caller.SendAsync("Error", "You are not authorized to send messages in this support request");
                    _logger.LogWarning("User {UserId} attempted unauthorized access to support request {SupportRequestId}",
                        senderId, dto.SupportRequestId.Value);
                    return;
                }
            }

            if (conversation == null)
            {
                // Ensure User1Id < User2Id for consistency
                var user1Id = senderId < dto.ReceiverId ? senderId : dto.ReceiverId;
                var user2Id = senderId < dto.ReceiverId ? dto.ReceiverId : senderId;

                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    CourseId = dto.CourseId,
                    User1Id = user1Id,
                    User2Id = user2Id,
                    LastMessageAt = DateTime.UtcNow,
                    LastMessagePreview = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = senderId
                };

                await _unitOfWork.Conversations.AddAsync(conversation);
            }

            // Create message
            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                CourseId = dto.CourseId,
                ConversationId = conversation.Id,
                Message = dto.Message.Trim(),
                SentAt = DateTime.UtcNow,
                IsDeleted = false,
                SupportRequestId = dto.SupportRequestId // NULL for general chat, <guid> for support request
            };

            await _unitOfWork.Chats.AddAsync(chat);

            // Update conversation
            var preview = dto.Message.Length > 100 ? dto.Message.Substring(0, 100) : dto.Message;
            conversation.LastMessagePreview = preview;
            conversation.LastMessageAt = chat.SentAt;

            await _unitOfWork.SaveChangesAsync();

            // Get user names
            var sender = await _userService.GetUserByIdAsync(senderId);
            var receiver = await _userService.GetUserByIdAsync(dto.ReceiverId);

            var messageDto = new Domain.DTOs.ChatMessageDto
            {
                Id = chat.Id,
                SenderId = senderId,
                SenderName = sender?.FullName ?? "Unknown",
                ReceiverId = dto.ReceiverId,
                ReceiverName = receiver?.FullName ?? "Unknown",
                Message = chat.Message,
                SentAt = chat.SentAt,
                IsDeleted = false
            };

            // Send to receiver if online
            if (_connections.TryGetValue(dto.ReceiverId, out var receiverConnections))
            {
                foreach (var connId in receiverConnections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage", messageDto);
                }
            }

            // Confirm to sender
            await Clients.Caller.SendAsync("MessageSent", messageDto);

            // ⭐⭐ NEW: notify receiver about updated unread count for this sender
            // Yêu cầu: Chats.CountUnreadMessagesAsync(courseId, senderId, receiverId)
            try
            {
                var unreadCountForReceiver = await _unitOfWork.Chats
                    .CountUnreadMessagesAsync(dto.CourseId, senderId, dto.ReceiverId);

                if (_connections.TryGetValue(dto.ReceiverId, out var receiverConnsForUnread))
                {
                    foreach (var connId in receiverConnsForUnread)
                    {
                        await Clients.Client(connId).SendAsync("UnreadCountChanged", new
                        {
                            userId = senderId,           // thằng gửi
                            unreadCount = unreadCountForReceiver
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Không fail cả SendMessage chỉ vì tính unread lỗi
                _logger.LogError(ex, "Failed to publish UnreadCountChanged");
            }

            _logger.LogInformation("Message sent from {SenderId} to {ReceiverId} in course {CourseId}",
                senderId, dto.ReceiverId, dto.CourseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    // ==================== TYPING INDICATORS ====================

    /// <summary>
    /// Notify receiver that sender is typing
    /// </summary>
    public async Task StartTyping(Guid receiverId)
    {
        var userId = _currentUserService.UserId!.Value;

        if (_connections.TryGetValue(receiverId, out var connections))
        {
            foreach (var connId in connections)
            {
                await Clients.Client(connId).SendAsync("UserTyping", new { userId, isTyping = true });
            }
        }
    }

    /// <summary>
    /// Notify receiver that sender stopped typing
    /// </summary>
    public async Task StopTyping(Guid receiverId)
    {
        var userId = _currentUserService.UserId!.Value;

        if (_connections.TryGetValue(receiverId, out var connections))
        {
            foreach (var connId in connections)
            {
                await Clients.Client(connId).SendAsync("UserTyping", new { userId, isTyping = false });
            }
        }
    }

    // ==================== READ RECEIPTS ====================

    /// <summary>
    /// Mark all unread messages in a conversation as read
    /// </summary>
    public async Task MarkMessagesAsRead(Guid conversationId)
    {
        var userId = _currentUserService.UserId!.Value;

        try
        {
            // Validate access to conversation
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                await Clients.Caller.SendAsync("Error", "Conversation not found");
                return;
            }

            if (conversation.User1Id != userId && conversation.User2Id != userId)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to this conversation");
                return;
            }

            // Mark messages as read
            await _unitOfWork.Chats.MarkAsReadAsync(conversationId, userId);
            await _unitOfWork.SaveChangesAsync();

            // Notify the sender that their messages were read
            var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

            if (_connections.TryGetValue(otherUserId, out var connections))
            {
                foreach (var connId in connections)
                {
                    await Clients.Client(connId).SendAsync("MessagesRead", new
                    {
                        conversationId = conversationId,
                        readBy = userId,
                        readAt = DateTime.UtcNow
                    });
                }
            }

            // NEW: notify the READER that unread from the other user is now 0 (or recalc)
            try
            {
                var unreadAfterRead = await _unitOfWork.Chats
                    .CountUnreadMessagesAsync(conversation.CourseId, otherUserId, userId);

                if (_connections.TryGetValue(userId, out var readerConns))
                {
                    foreach (var connId in readerConns)
                    {
                        await Clients.Client(connId).SendAsync("UnreadCountChanged", new
                        {
                            userId = otherUserId,       
                            unreadCount = unreadAfterRead
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish UnreadCountChanged after MarkMessagesAsRead");
            }

            _logger.LogInformation("User {UserId} marked messages as read in conversation {ConversationId}",
                userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read");
            await Clients.Caller.SendAsync("Error", "Failed to mark messages as read");
        }
    }

    // ==================== SUPPORT REQUEST METHODS ====================

    /// <summary>
    /// Staff joins the support room to receive support request notifications
    /// </summary>
    public async Task JoinSupportRoom()
    {
        var userId = _currentUserService.UserId!.Value;

        // Verify user is staff
        var isStaff = await _userService.ValidateUserAsync(userId, "Staff");
        if (!isStaff)
        {
            await Clients.Caller.SendAsync("Error", "Only staff can join the support room");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "SupportStaff");
        _supportStaffConnections.TryAdd(Context.ConnectionId, userId);

        _logger.LogInformation("Staff {UserId} joined support room", userId);
        await Clients.Caller.SendAsync("JoinedSupportRoom", new { success = true });
    }

    /// <summary>
    /// Staff leaves the support room
    /// </summary>
    public async Task LeaveSupportRoom()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SupportStaff");
        _supportStaffConnections.TryRemove(Context.ConnectionId, out _);

        var userId = _currentUserService.UserId!.Value;
        _logger.LogInformation("Staff {UserId} left support room", userId);

        await Clients.Caller.SendAsync("LeftSupportRoom", new { success = true });
    }

    /// <summary>
    /// Broadcast a new support request to all staff in support room
    /// This is called internally when a support request is created
    /// </summary>
    public async Task NotifyNewSupportRequest(object supportRequestData)
    {
        await Clients.Group("SupportStaff").SendAsync("NewSupportRequest", supportRequestData);
    }

    /// <summary>
    /// Notify requester that their support request was accepted
    /// </summary>
    public async Task NotifySupportRequestAccepted(Guid requesterId, object acceptedData)
    {
        if (_connections.TryGetValue(requesterId, out var connections))
        {
            foreach (var connId in connections)
            {
                await Clients.Client(connId).SendAsync("SupportRequestAccepted", acceptedData);
            }
        }
    }

    /// <summary>
    /// Notify both parties that support request was resolved
    /// </summary>
    public async Task NotifySupportRequestResolved(Guid requesterId, Guid? staffId, object resolvedData)
    {
        // Notify requester
        if (_connections.TryGetValue(requesterId, out var requesterConnections))
        {
            foreach (var connId in requesterConnections)
            {
                await Clients.Client(connId).SendAsync("SupportRequestResolved", resolvedData);
            }
        }

        // Notify staff if assigned
        if (staffId.HasValue && _connections.TryGetValue(staffId.Value, out var staffConnections))
        {
            foreach (var connId in staffConnections)
            {
                await Clients.Client(connId).SendAsync("SupportRequestResolved", resolvedData);
            }
        }
    }

    // ==================== HELPERS ====================

    private async Task<bool> HasCourseAccess(Guid userId, Guid courseId)
    {
        // Check enrollment (for students)
        var enrollment = await _unitOfWork.CourseEnrollments
            .GetEnrollmentAsync(courseId, userId);
        if (enrollment != null) return true;

        // Check if lecturer of the course
        var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
        if (course?.LecturerId == userId) return true;

        // Check if the specific user has staff role (not just current user)
        var isStaff = await _userService.ValidateUserAsync(userId, "Staff");
        if (isStaff) return true;

        return false;
    }
}
