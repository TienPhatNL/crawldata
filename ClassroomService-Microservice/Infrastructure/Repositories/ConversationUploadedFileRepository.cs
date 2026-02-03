using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ConversationUploadedFile
/// </summary>
public class ConversationUploadedFileRepository : Repository<ConversationUploadedFile>, IConversationUploadedFileRepository
{
    public ConversationUploadedFileRepository(ClassroomDbContext context) : base(context)
    {
    }

    public async Task<List<ConversationUploadedFile>> GetByConversationIdAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ConversationUploadedFiles
            .Where(f => f.ConversationId == conversationId && !f.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConversationUploadedFile>> GetByConversationIdOrderedAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.ConversationUploadedFiles
            .Where(f => f.ConversationId == conversationId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }
}
