using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class CrawlerChatMessageRepository : Repository<CrawlerChatMessage>, ICrawlerChatMessageRepository
{
    public CrawlerChatMessageRepository(ClassroomDbContext context) : base(context)
    {
    }

    public new async Task<CrawlerChatMessage?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Assignment)
            .Include(m => m.Group)
            .Include(m => m.ParentMessage)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<CrawlerChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CrawlerChatMessage?> GetByCrawlJobIdAsync(
        Guid crawlJobId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.CrawlJobId == crawlJobId, cancellationToken);
    }

    public async Task<List<CrawlerChatMessage>> GetByAssignmentIdAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.AssignmentId == assignmentId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CrawlerChatMessage>> GetByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CrawlerChatMessage>> GetConversationHistoryAsync(
        Guid conversationId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unreadMessages = await _dbSet
            .Where(m => m.ConversationId == conversationId &&
                       m.SenderId != userId &&
                       !m.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }
    }
}
