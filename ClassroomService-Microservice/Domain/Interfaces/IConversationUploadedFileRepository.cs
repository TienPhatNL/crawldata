using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Repository for managing ConversationUploadedFile entities
/// </summary>
public interface IConversationUploadedFileRepository : IRepository<ConversationUploadedFile>
{
    /// <summary>
    /// Get all uploaded files for a specific conversation (excluding deleted)
    /// </summary>
    Task<List<ConversationUploadedFile>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get uploaded files by conversation ID ordered by upload date (newest first)
    /// </summary>
    Task<List<ConversationUploadedFile>> GetByConversationIdOrderedAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
