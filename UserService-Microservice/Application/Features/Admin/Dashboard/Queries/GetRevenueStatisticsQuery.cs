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

public class GetRevenueStatisticsQuery : IRequest<ResponseModel>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Interval { get; set; } = "day"; // day, week, month
}

public class GetRevenueStatisticsQueryHandler : IRequestHandler<GetRevenueStatisticsQuery, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly IDistributedCache _cache;

    public GetRevenueStatisticsQueryHandler(UserDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(GetRevenueStatisticsQuery request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var startDate = request.StartDate ?? endDate.AddDays(-30);

        // Check cache
        var cacheKey = $"admin:dashboard:revenue:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{request.Interval}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResult = JsonSerializer.Deserialize<RevenueStatisticsDto>(cachedData);
            return new ResponseModel
            {
                Status = HttpStatusCode.OK,
                Data = cachedResult
            };
        }

        // Get all paid payments in the period
        var payments = await _context.SubscriptionPayments
            .Include(p => p.SubscriptionPlan)
            .Where(p => p.Status == SubscriptionPaymentStatus.Paid && 
                       p.PaidAt >= startDate && 
                       p.PaidAt <= endDate)
            .ToListAsync(cancellationToken);

        // Calculate total revenue
        var totalRevenue = payments.Sum(p => p.Amount);

        // Revenue by tier
        var revenueByTier = payments
            .Where(p => p.SubscriptionPlan != null && p.SubscriptionPlan.Tier != null)
            .GroupBy(p => p.SubscriptionPlan!.Tier!.Name)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        // Calculate timeline
        var timeline = GroupPaymentsByInterval(payments, startDate, endDate, request.Interval);

        // Calculate growth (compare to previous period)
        var periodLength = (endDate - startDate).Days;
        var previousStartDate = startDate.AddDays(-periodLength);
        var previousEndDate = startDate;

        var previousRevenue = await _context.SubscriptionPayments
            .Where(p => p.Status == SubscriptionPaymentStatus.Paid &&
                       p.PaidAt >= previousStartDate &&
                       p.PaidAt < previousEndDate)
            .SumAsync(p => p.Amount, cancellationToken);

        var growth = new RevenueGrowthDto
        {
            CurrentPeriodRevenue = totalRevenue,
            PreviousPeriodRevenue = previousRevenue,
            Percentage = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0,
            ComparedTo = "previous_period"
        };

        // Calculate averages
        var totalUsers = await _context.Users
            .Where(u => u.CurrentSubscriptionPlanId != null)
            .CountAsync(cancellationToken);

        var statistics = new RevenueStatisticsDto
        {
            TotalRevenue = totalRevenue,
            Currency = "VND",
            RevenueByTier = revenueByTier,
            Timeline = timeline,
            Growth = growth,
            AverageRevenuePerUser = totalUsers > 0 ? totalRevenue / totalUsers : 0,
            AverageOrderValue = payments.Count > 0 ? totalRevenue / payments.Count : 0
        };

        // Cache for 10 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(statistics), cacheOptions, cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Revenue statistics retrieved successfully", statistics);
    }

    private List<RevenueTimelineItem> GroupPaymentsByInterval(
        List<Domain.Entities.SubscriptionPayment> payments,
        DateTime startDate,
        DateTime endDate,
        string interval)
    {
        var timeline = new List<RevenueTimelineItem>();

        if (interval == "day")
        {
            var grouped = payments
                .GroupBy(p => p.PaidAt!.Value.Date)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                timeline.Add(new RevenueTimelineItem
                {
                    Date = group.Key,
                    Amount = group.Sum(p => p.Amount),
                    OrderCount = group.Count()
                });
            }
        }
        else if (interval == "week")
        {
            var grouped = payments
                .GroupBy(p => GetWeekStart(p.PaidAt!.Value))
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                timeline.Add(new RevenueTimelineItem
                {
                    Date = group.Key,
                    Amount = group.Sum(p => p.Amount),
                    OrderCount = group.Count()
                });
            }
        }
        else // month
        {
            var grouped = payments
                .GroupBy(p => new DateTime(p.PaidAt!.Value.Year, p.PaidAt.Value.Month, 1))
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                timeline.Add(new RevenueTimelineItem
                {
                    Date = group.Key,
                    Amount = group.Sum(p => p.Amount),
                    OrderCount = group.Count()
                });
            }
        }

        return timeline;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
