namespace WebCrawlerService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICrawlJobRepository CrawlJobs { get; }
    ICrawlerAgentRepository CrawlerAgents { get; }
    IDomainPolicyRepository DomainPolicies { get; }
    ICrawlResultRepository CrawlResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}