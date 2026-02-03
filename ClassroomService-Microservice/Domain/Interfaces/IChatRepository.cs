using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository interface for Chat-specific operations
/// </summary>
public interface IChatRepository : IRepository<Chat>
{
    /// <summary>
    /// Get all chat messages for a specific course
    /// </summary>
    Task<IEnumerable<Chat>> GetChatsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat messages sent by a specific user
    /// </summary>
    Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat messages between two users
    /// </summary>
    Task<IEnumerable<Chat>> GetChatsBetweenUsersAsync(Guid senderId, Guid receiverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent chat messages for a course (with limit)
    /// </summary>
    Task<IEnumerable<Chat>> GetRecentChatsAsync(Guid courseId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat messages for a course within a date range
    /// </summary>
    Task<IEnumerable<Chat>> GetChatsByDateRangeAsync(Guid courseId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paginated messages in a conversation, optionally filtered by support request ID
    /// </summary>
    Task<List<Chat>> GetConversationMessagesAsync(
        Guid conversationId, 
        int pageNumber = 1, 
        int pageSize = 50,
        Guid? supportRequestId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count messages in a conversation
    /// </summary>
    Task<int> CountConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark all unread messages in a conversation as read for a specific user
    /// </summary>
    Task MarkAsReadAsync(
        Guid conversationId, 
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count unread messages in a conversation for a specific user
    /// </summary>
    Task<int> CountUnreadAsync(
        Guid conversationId, 
        Guid userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get unread message counts for all conversations for a user
    /// Returns dictionary of ConversationId -> UnreadCount
    /// </summary>
    Task<Dictionary<Guid, int>> GetUnreadCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadMessagesAsync(Guid courseId, Guid senderId, Guid receiverId, CancellationToken cancellationToken = default);
}
