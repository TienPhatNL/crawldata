using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(UserDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByIdWithPlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.CurrentSubscriptionPlan)
                .ThenInclude(p => p.Tier) // Include SubscriptionTier entity
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.CurrentSubscription)
            .Include(u => u.CurrentSubscriptionPlan)
                .ThenInclude(p => p.Tier) // Include SubscriptionTier entity
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => 
            u.EmailVerificationToken == token && 
            u.EmailVerificationTokenExpires > DateTime.UtcNow, 
            cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow,
            cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Role == role)
            .Include(u => u.CurrentSubscription)
            .Include(u => u.CurrentSubscriptionPlan)
                .ThenInclude(p => p.Tier) // Include SubscriptionTier entity
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersRequiringApprovalAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Status == UserStatus.PendingApproval)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<User>> GetUsersByStatusAsync(UserStatus status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(
            filter: u => u.Status == status,
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            pageNumber: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken,
            includes: u => u.CurrentSubscription
        );
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        return user?.IsEmailConfirmed ?? false;
    }

    public async Task<int> GetUserCountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await CountAsync(u => u.Role == role, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(timeSpan);
        return await _dbSet
            .Where(u => u.CreatedAt >= cutoffDate)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        var emailList = emails.Select(e => e.ToLower()).ToList();
        return await _dbSet
            .Include(u => u.CurrentSubscription)
            .Include(u => u.CurrentSubscriptionPlan)
                .ThenInclude(p => p.Tier) // Include SubscriptionTier entity
            .Where(u => emailList.Contains(u.Email.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        UserRole? role = null,
        UserStatus? status = null,
        string? subscriptionTierName = null,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.CurrentSubscription)
            .Include(u => u.CurrentSubscriptionPlan)
                .ThenInclude(p => p.Tier) // Include SubscriptionTier entity
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(u => 
                u.Email.ToLower().Contains(lowerSearchTerm) ||
                u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                u.LastName.ToLower().Contains(lowerSearchTerm) ||
                (u.InstitutionName != null && u.InstitutionName.ToLower().Contains(lowerSearchTerm)));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(subscriptionTierName))
        {
            query = query.Where(u => u.SubscriptionTier != null && u.SubscriptionTier.Name.ToLower() == subscriptionTierName.ToLower());
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";
            
            query = sortBy.ToLower() switch
            {
                "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "firstname" => isDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                "lastname" => isDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                "role" => isDescending ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
                "status" => isDescending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
                "lastloginat" => isDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                "createdat" or _ => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(u => u.CreatedAt);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }
}