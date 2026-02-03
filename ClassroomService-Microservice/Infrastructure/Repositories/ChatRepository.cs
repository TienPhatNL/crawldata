using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class ChatRepository : Repository<Chat>, IChatRepository
{
    public ChatRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Chat>> GetChatsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.CourseId == courseId)
            .Include(c => c.Course)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.SenderId == userId || c.ReceiverId == userId)
            .Include(c => c.Course)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetChatsBetweenUsersAsync(Guid senderId, Guid receiverId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => (c.SenderId == senderId && c.ReceiverId == receiverId) ||
                       (c.SenderId == receiverId && c.ReceiverId == senderId))
            .Include(c => c.Course)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetRecentChatsAsync(Guid courseId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.CourseId == courseId)
            .Include(c => c.Course)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetChatsByDateRangeAsync(Guid courseId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.CourseId == courseId && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .Include(c => c.Course)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Chat>> GetConversationMessagesAsync(
        Guid conversationId, 
        int pageNumber = 1, 
        int pageSize = 50,
        Guid? supportRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(c => c.ConversationId == conversationId && !c.IsDeleted);

        // Filter by support request ID if provided (BEFORE pagination)
        if (supportRequestId.HasValue)
        {
            query = query.Where(c => c.SupportRequestId == supportRequestId.Value);
        }

        return await query
            .OrderByDescending(c => c.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.ConversationId == conversationId && !c.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid conversationId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var unreadMessages = await _dbSet
            .Where(m => m.ConversationId == conversationId &&
                       m.ReceiverId == userId &&
                       !m.IsRead &&
                       !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }
    }

    public async Task<int> CountUnreadAsync(
        Guid conversationId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId &&
                       m.ReceiverId == userId &&
                       !m.IsRead &&
                       !m.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count, cancellationToken);
    }
    public async Task<int> CountUnreadMessagesAsync(
        Guid courseId,
        Guid senderId,
        Guid receiverId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m =>
                m.CourseId == courseId &&
                m.SenderId == senderId &&
                m.ReceiverId == receiverId &&
                !m.IsRead &&
                !m.IsDeleted)
            .CountAsync(cancellationToken);
    }
}
