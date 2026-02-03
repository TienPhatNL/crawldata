using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern for managing transactions and repository access
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    ICourseRepository Courses { get; }
    ICourseEnrollmentRepository CourseEnrollments { get; }
    IAssignmentRepository Assignments { get; }
    IRepository<Topic> Topics { get; }
    IGroupRepository Groups { get; }
    IGroupMemberRepository GroupMembers { get; }
    ITermRepository Terms { get; }
    ICourseCodeRepository CourseCodes { get; }
    ICourseRequestRepository CourseRequests { get; }
    IChatRepository Chats { get; }
    IConversationRepository Conversations { get; }
    IReportRepository Reports { get; }
    ICrawlerChatMessageRepository CrawlerChatMessages { get; }
    IReportHistoryRepository ReportHistory { get; }
    ISupportRequestRepository SupportRequests { get; }
    IReportAICheckRepository ReportAIChecks { get; }
    IConversationCrawlDataRepository ConversationCrawlData { get; }
    IConversationUploadedFileRepository ConversationUploadedFiles { get; }

    /// <summary>
    /// Save all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation within a transaction
    /// </summary>
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation within a transaction and return a result
    /// </summary>
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
