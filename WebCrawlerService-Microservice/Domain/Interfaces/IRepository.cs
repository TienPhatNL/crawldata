using System.Linq.Expressions;
using WebCrawlerService.Domain.Common;

namespace WebCrawlerService.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
    
    // Advanced querying with pagination and ordering
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes);

    Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes);

    // Queryable for complex operations
    IQueryable<T> GetQueryable(
        Expression<Func<T, bool>>? filter = null,
        params Expression<Func<T, object>>[] includes);

    // Find operations
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    // Existence and counting
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);

    // Modification operations
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    T Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    // Bulk operations - remove problematic method for now
    Task<int> DeleteManyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
}