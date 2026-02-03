using System.Linq.Expressions;
using ClassroomService.Domain.Common;

namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD operations
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Get an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an entity by its ID with navigation property includes
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Get a single entity matching the predicate
    /// </summary>
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single entity matching the predicate with navigation property includes
    /// </summary>
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with navigation property includes
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Get multiple entities matching the predicate
    /// </summary>
    Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple entities matching the predicate with navigation property includes
    /// </summary>
    Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Get paginated results with filtering, ordering, and includes
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Check if any entity matches the predicate
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count entities matching the predicate
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity by a specific property value
    /// </summary>
    Task<T?> GetByPropertyAsync<TProperty>(Expression<Func<T, TProperty>> propertySelector, TProperty value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get single entity by a specific property value (throws if more than one)
    /// </summary>
    Task<T?> GetSingleByPropertyAsync<TProperty>(Expression<Func<T, TProperty>> propertySelector, TProperty value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update multiple entities
    /// </summary>
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity (soft delete if supported)
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity by ID (soft delete if supported)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple entities (soft delete if supported)
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
}
