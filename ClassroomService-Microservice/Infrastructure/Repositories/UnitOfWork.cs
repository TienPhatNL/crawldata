using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;

namespace ClassroomService.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repository access
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ClassroomDbContext _context;
    private readonly IDomainEventService _domainEventService;
    private bool _disposed = false;

    // Repository instances (lazy initialization)
    private ICourseRepository? _courses;
    private ICourseEnrollmentRepository? _courseEnrollments;
    private IAssignmentRepository? _assignments;
    private IRepository<Topic>? _topics;
    private IGroupRepository? _groups;
    private IGroupMemberRepository? _groupMembers;
    private ITermRepository? _terms;
    private ICourseCodeRepository? _courseCodes;
    private ICourseRequestRepository? _courseRequests;
    private IChatRepository? _chats;
    private IConversationRepository? _conversations;
    private IReportRepository? _reports;
    private ICrawlerChatMessageRepository? _crawlerChatMessages;
    private IReportHistoryRepository? _reportHistory;
    private ISupportRequestRepository? _supportRequests;
    private IReportAICheckRepository? _reportAIChecks;
    private IConversationCrawlDataRepository? _conversationCrawlData;
    private IConversationUploadedFileRepository? _conversationUploadedFiles;

    public UnitOfWork(ClassroomDbContext context, IDomainEventService domainEventService)
    {
        _context = context;
        _domainEventService = domainEventService;
    }

    // Repository properties with lazy initialization
    public ICourseRepository Courses =>
        _courses ??= new CourseRepository(_context);

    public ICourseEnrollmentRepository CourseEnrollments =>
        _courseEnrollments ??= new CourseEnrollmentRepository(_context);

    public IAssignmentRepository Assignments =>
        _assignments ??= new AssignmentRepository(_context);

    public IRepository<Topic> Topics =>
        _topics ??= new Repository<Topic>(_context);

    public IGroupRepository Groups =>
        _groups ??= new GroupRepository(_context);

    public IGroupMemberRepository GroupMembers =>
        _groupMembers ??= new GroupMemberRepository(_context);

    public ITermRepository Terms =>
        _terms ??= new TermRepository(_context);

    public ICourseCodeRepository CourseCodes =>
        _courseCodes ??= new CourseCodeRepository(_context);

    public ICourseRequestRepository CourseRequests =>
        _courseRequests ??= new CourseRequestRepository(_context);

    public IChatRepository Chats =>
        _chats ??= new ChatRepository(_context);

    public IConversationRepository Conversations =>
        _conversations ??= new ConversationRepository(_context);

    public IReportRepository Reports =>
        _reports ??= new ReportRepository(_context);

    public ICrawlerChatMessageRepository CrawlerChatMessages =>
        _crawlerChatMessages ??= new CrawlerChatMessageRepository(_context);

    public IReportHistoryRepository ReportHistory =>
        _reportHistory ??= new ReportHistoryRepository(_context);

    public ISupportRequestRepository SupportRequests =>
        _supportRequests ??= new SupportRequestRepository(_context);

    public IReportAICheckRepository ReportAIChecks =>
        _reportAIChecks ??= new ReportAICheckRepository(_context);

    public IConversationCrawlDataRepository ConversationCrawlData =>
        _conversationCrawlData ??= new ConversationCrawlDataRepository(_context);

    public IConversationUploadedFileRepository ConversationUploadedFiles =>
        _conversationUploadedFiles ??= new ConversationUploadedFileRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await DispatchDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await DispatchDomainEventsAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        var domainEvents = entities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        entities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _domainEventService.PublishAsync(domainEvent, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
