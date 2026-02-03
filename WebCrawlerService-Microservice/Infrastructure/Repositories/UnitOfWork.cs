using Microsoft.EntityFrameworkCore.Storage;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly CrawlerDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Repository instances
    private ICrawlJobRepository? _crawlJobs;
    private ICrawlerAgentRepository? _crawlerAgents;
    private IDomainPolicyRepository? _domainPolicies;
    private ICrawlResultRepository? _crawlResults;

    public UnitOfWork(CrawlerDbContext context)
    {
        _context = context;
    }

    public ICrawlJobRepository CrawlJobs =>
        _crawlJobs ??= new CrawlJobRepository(_context);

    public ICrawlerAgentRepository CrawlerAgents =>
        _crawlerAgents ??= new CrawlerAgentRepository(_context);

    public IDomainPolicyRepository DomainPolicies =>
        _domainPolicies ??= new DomainPolicyRepository(_context);

    public ICrawlResultRepository CrawlResults =>
        _crawlResults ??= new CrawlResultRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }
}