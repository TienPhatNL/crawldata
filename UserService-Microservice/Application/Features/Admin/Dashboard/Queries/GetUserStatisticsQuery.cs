using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UserService.Application.Common.Models;
using UserService.Application.Features.Admin.Dashboard.DTOs;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.Admin.Dashboard.Queries;

public class GetUserStatisticsQuery : IRequest<ResponseModel>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int QuotaThreshold { get; set; } = 80; // Users using > 80% quota
}

public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly IDistributedCache _cache;

    public GetUserStatisticsQueryHandler(UserDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var startDate = request.StartDate ?? endDate.AddDays(-30);

        // Check cache
        var cacheKey = $"admin:dashboard:users:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{request.QuotaThreshold}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResult = JsonSerializer.Deserialize<UserStatisticsDto>(cachedData);
            return new ResponseModel
            {
                Status = HttpStatusCode.OK,
                Data = cachedResult
            };
        }

        // Get total users
        var totalUsers = await _context.Users.CountAsync(cancellationToken);

        // Users by tier
        var allUsers = await _context.Users
            .Include(u => u.CurrentSubscriptionPlan)
            .ToListAsync(cancellationToken);

        var usersByTier = allUsers
            .Where(u => u.SubscriptionTier != null)
            .GroupBy(u => u.SubscriptionTier!.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // Users by role
        var usersByRole = allUsers
            .GroupBy(u => u.Role)
            .ToDictionary(g => g.Key, g => g.Count());

        // New users in period
        var newUsers = await _context.Users
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
            .CountAsync(cancellationToken);

        // Active users (with activity in last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var activeUsers = await _context.Users
            .Where(u => u.UpdatedAt >= thirtyDaysAgo || u.LastLoginAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        // Conversion rate (users with paid plans / total users * 100)
        var paidUsers = allUsers.Count(u => u.SubscriptionTier != null && u.SubscriptionTier.Level > 0);
        var conversionRate = totalUsers > 0 ? (decimal)paidUsers / totalUsers * 100 : 0;

        // Average lifetime value (total revenue / total users)
        var totalRevenue = await _context.SubscriptionPayments
            .Where(p => p.Status == SubscriptionPaymentStatus.Paid)
            .SumAsync(p => p.Amount, cancellationToken);

        var averageLifetimeValue = totalUsers > 0 ? totalRevenue / totalUsers : 0;

        // Timeline
        var timeline = await BuildUserGrowthTimeline(startDate, endDate, cancellationToken);

        // Users near quota limit
        var usersNearQuota = await _context.Users
            .Include(u => u.CurrentSubscriptionPlan)
            .Where(u => u.CurrentSubscriptionPlanId != null && u.CurrentSubscriptionPlan != null)
            .ToListAsync(cancellationToken);

        var nearQuotaList = usersNearQuota
            .Where(u => u.CurrentSubscriptionPlan!.QuotaLimit > 0)
            .Select(u => new
            {
                User = u,
                UsagePercentage = (decimal)u.CrawlQuotaUsed / u.CurrentSubscriptionPlan!.QuotaLimit * 100
            })
            .Where(x => x.UsagePercentage >= request.QuotaThreshold)
            .OrderByDescending(x => x.UsagePercentage)
            .Take(50)
            .Select(x => new UserNearQuotaLimit
            {
                UserId = x.User.Id,
                Email = x.User.Email,
                CurrentTier = x.User.SubscriptionTier?.Name ?? "Free",
                QuotaUsed = x.User.CrawlQuotaUsed,
                QuotaLimit = x.User.CurrentSubscriptionPlan!.QuotaLimit,
                UsagePercentage = x.UsagePercentage
            })
            .ToList();

        var statistics = new UserStatisticsDto
        {
            TotalUsers = totalUsers,
            UsersByTier = usersByTier,
            UsersByRole = usersByRole,
            NewUsers = newUsers,
            ActiveUsers = activeUsers,
            ConversionRate = conversionRate,
            AverageLifetimeValue = averageLifetimeValue,
            Timeline = timeline,
            UsersNearQuota = nearQuotaList
        };

        // Cache for 10 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(statistics), cacheOptions, cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "User statistics retrieved successfully", statistics);
    }

    private async Task<List<UserGrowthTimelineItem>> BuildUserGrowthTimeline(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var timeline = new List<UserGrowthTimelineItem>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(7))
        {
            var weekEnd = date.AddDays(7);

            // Count users created in this week
            var newUsersCount = await _context.Users
                .Where(u => u.CreatedAt >= date && u.CreatedAt < weekEnd)
                .CountAsync(cancellationToken);

            // Count total users created up to the end of this week
            var totalUsersCount = await _context.Users
                .Where(u => u.CreatedAt < weekEnd)
                .CountAsync(cancellationToken);

            // Count paid users at this point in time (users with subscriptions created before weekEnd)
            var paidUsersCount = await _context.Users
                .Where(u => u.CreatedAt < weekEnd)
                .Join(
                    _context.UserSubscriptions.Where(s => s.CreatedAt < weekEnd && (s.CancelledAt == null || s.CancelledAt >= weekEnd)),
                    user => user.Id,
                    sub => sub.UserId,
                    (user, sub) => sub
                )
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.Tier)
                .Where(s => s.SubscriptionPlan.Tier.Level > 0)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            timeline.Add(new UserGrowthTimelineItem
            {
                Date = date,
                NewUsers = newUsersCount,
                TotalUsers = totalUsersCount,
                PaidUsers = paidUsersCount
            });
        }

        return timeline;
    }
}
