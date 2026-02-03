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

public class GetSubscriptionStatisticsQuery : IRequest<ResponseModel>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetSubscriptionStatisticsQueryHandler : IRequestHandler<GetSubscriptionStatisticsQuery, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly IDistributedCache _cache;

    public GetSubscriptionStatisticsQueryHandler(UserDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionStatisticsQuery request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var startDate = request.StartDate ?? endDate.AddDays(-30);

        // Check cache
        var cacheKey = $"admin:dashboard:subscriptions:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResult = JsonSerializer.Deserialize<SubscriptionStatisticsDto>(cachedData);
            return new ResponseModel
            {
                Status = HttpStatusCode.OK,
                Data = cachedResult
            };
        }

        // Active subscriptions (users with active subscription plans)
        var activeUsers = await _context.Users
            .Include(u => u.CurrentSubscriptionPlan)
            .Where(u => u.CurrentSubscriptionPlanId != null && u.CurrentSubscriptionPlan != null)
            .ToListAsync(cancellationToken);

        var totalActiveSubscriptions = activeUsers.Count;

        // Subscriptions by tier
        var subscriptionsByTier = activeUsers
            .Where(u => u.CurrentSubscriptionPlan != null && u.CurrentSubscriptionPlan.Tier != null)
            .GroupBy(u => u.CurrentSubscriptionPlan!.Tier!.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // New subscriptions in period (filter by CreatedAt, not StartDate)
        var newSubscriptions = await _context.UserSubscriptions
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .CountAsync(cancellationToken);

        // Cancelled subscriptions in period
        var cancelledSubscriptions = await _context.UserSubscriptions
            .Where(s => s.CancelledAt >= startDate && s.CancelledAt <= endDate)
            .CountAsync(cancellationToken);

        // Churn rate (cancelled / active * 100)
        var churnRate = totalActiveSubscriptions > 0
            ? (decimal)cancelledSubscriptions / totalActiveSubscriptions * 100
            : 0;

        // Renewal rate (renewed / expired * 100)
        var expiredSubscriptions = await _context.UserSubscriptions
            .Where(s => s.EndDate >= startDate && s.EndDate <= endDate && s.EndDate < DateTime.UtcNow)
            .CountAsync(cancellationToken);

        var renewedSubscriptions = await _context.UserSubscriptions
            .Where(s => s.EndDate >= startDate && s.EndDate <= endDate && s.AutoRenew)
            .CountAsync(cancellationToken);

        var renewalRate = expiredSubscriptions > 0
            ? (decimal)renewedSubscriptions / expiredSubscriptions * 100
            : 0;

        // Upgrade/Downgrade stats
        var subscriptionHistory = await _context.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var upgrades = 0;
        var downgrades = 0;

        // Simplified upgrade/downgrade tracking based on tier changes
        var userSubscriptionChanges = subscriptionHistory
            .GroupBy(s => s.UserId)
            .Where(g => g.Count() > 1);

        foreach (var userSubs in userSubscriptionChanges)
        {
            var ordered = userSubs.OrderBy(s => s.CreatedAt).ToList();
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].SubscriptionPlan?.Tier?.Level > ordered[i - 1].SubscriptionPlan?.Tier?.Level)
                    upgrades++;
                else if (ordered[i].SubscriptionPlan?.Tier?.Level < ordered[i - 1].SubscriptionPlan?.Tier?.Level)
                    downgrades++;
            }
        }

        // Timeline
        var timeline = await BuildSubscriptionTimeline(startDate, endDate, cancellationToken);

        // Average subscription value
        var paidPlans = await _context.SubscriptionPlans
            .Where(p => p.Price > 0 && p.IsActive)
            .ToListAsync(cancellationToken);

        var averageSubscriptionValue = paidPlans.Any() ? paidPlans.Average(p => p.Price) : 0;

        var statistics = new SubscriptionStatisticsDto
        {
            TotalActiveSubscriptions = totalActiveSubscriptions,
            SubscriptionsByTier = subscriptionsByTier,
            NewSubscriptions = newSubscriptions,
            ChurnRate = churnRate,
            RenewalRate = renewalRate,
            UpgradeDowngrade = new UpgradeDowngradeStats
            {
                Upgrades = upgrades,
                Downgrades = downgrades,
                NetChange = upgrades - downgrades
            },
            Timeline = timeline,
            AverageSubscriptionValue = averageSubscriptionValue
        };

        // Cache for 15 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(statistics), cacheOptions, cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Subscription statistics retrieved successfully", statistics);
    }

    private async Task<List<SubscriptionTimelineItem>> BuildSubscriptionTimeline(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var timeline = new List<SubscriptionTimelineItem>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(7))
        {
            var weekEnd = date.AddDays(7);

            var newSubs = await _context.UserSubscriptions
                .Where(s => s.CreatedAt >= date && s.CreatedAt < weekEnd)
                .CountAsync(cancellationToken);

            var cancelled = await _context.UserSubscriptions
                .Where(s => s.CancelledAt >= date && s.CancelledAt < weekEnd)
                .CountAsync(cancellationToken);

            // Active subscriptions at this point in time
            var activeTotal = await _context.UserSubscriptions
                .Where(s => s.CreatedAt < weekEnd && (s.CancelledAt == null || s.CancelledAt >= weekEnd))
                .CountAsync(cancellationToken);

            timeline.Add(new SubscriptionTimelineItem
            {
                Date = date,
                NewSubscriptions = newSubs,
                CancelledSubscriptions = cancelled,
                ActiveTotal = activeTotal
            });
        }

        return timeline;
    }
}
