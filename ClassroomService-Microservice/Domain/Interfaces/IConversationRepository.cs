using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    /// <summary>
    /// Get existing conversation between two users in a course
    /// </summary>
    Task<Conversation?> GetConversationAsync(
        Guid courseId, Guid userId1, Guid userId2,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    Task<List<Conversation>> GetUserConversationsAsync(
        Guid userId, Guid? courseId = null,
        CancellationToken cancellationToken = default);
}
