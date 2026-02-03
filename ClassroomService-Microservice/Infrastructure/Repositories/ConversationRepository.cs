using Microsoft.EntityFrameworkCore;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetConversationAsync(
        Guid courseId, Guid userId1, Guid userId2,
        CancellationToken cancellationToken = default)
    {
        // Ensure User1Id < User2Id for consistent ordering
        var (user1, user2) = userId1.CompareTo(userId2) < 0 
            ? (userId1, userId2) 
            : (userId2, userId1);

        return await _dbSet
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => 
                c.CourseId == courseId && 
                c.User1Id == user1 && 
                c.User2Id == user2 &&
                !c.IsCrawler,
                cancellationToken);
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(
        Guid userId, Guid? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(c => c.Course)
            .Where(c => (c.User1Id == userId || c.User2Id == userId) && !c.IsCrawler);

        if (courseId.HasValue)
        {
            query = query.Where(c => c.CourseId == courseId.Value);
        }

        return await query
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);
    }
}
