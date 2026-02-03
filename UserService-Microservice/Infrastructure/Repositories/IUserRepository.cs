using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.Infrastructure.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdWithPlanAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersRequiringApprovalAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<User>> GetUsersByStatusAsync(UserStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUserCountByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        UserRole? role = null,
        UserStatus? status = null,
        string? subscriptionTierName = null,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default);
}