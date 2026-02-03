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

public class GetPaymentStatisticsQuery : IRequest<ResponseModel>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Interval { get; set; } = "day";
}

public class GetPaymentStatisticsQueryHandler : IRequestHandler<GetPaymentStatisticsQuery, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly IDistributedCache _cache;

    public GetPaymentStatisticsQueryHandler(UserDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(GetPaymentStatisticsQuery request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var startDate = request.StartDate ?? endDate.AddDays(-30);

        // Check cache
        var cacheKey = $"admin:dashboard:payments:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{request.Interval}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedResult = JsonSerializer.Deserialize<PaymentStatisticsDto>(cachedData);
            return new ResponseModel
            {
                Status = HttpStatusCode.OK,
                Data = cachedResult
            };
        }

        // Get all payments
        var allPayments = await _context.SubscriptionPayments
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var totalOrders = allPayments.Count;

        // New orders (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var newOrders = allPayments.Count(p => p.CreatedAt >= sevenDaysAgo);

        // Status distribution
        var statusDistribution = allPayments
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Success rate
        var successCount = allPayments.Count(p => p.Status == SubscriptionPaymentStatus.Paid);
        var successRate = totalOrders > 0 ? (decimal)successCount / totalOrders * 100 : 0;

        // Failed payments analysis
        var failedPayments = allPayments
            .Where(p => p.Status == SubscriptionPaymentStatus.Failed)
            .GroupBy(p => p.FailureReason ?? "Unknown")
            .Select(g => new FailedPaymentInfo
            {
                Reason = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        // Timeline
        var timeline = GroupPaymentsByInterval(allPayments, request.Interval);

        // Average processing time (for paid payments)
        var paidPayments = allPayments.Where(p => p.Status == SubscriptionPaymentStatus.Paid && p.PaidAt.HasValue);
        var averageProcessingTime = paidPayments.Any()
            ? (decimal)paidPayments.Average(p => (p.PaidAt!.Value - p.CreatedAt).TotalSeconds)
            : 0;

        var statistics = new PaymentStatisticsDto
        {
            TotalOrders = totalOrders,
            NewOrders = newOrders,
            StatusDistribution = statusDistribution,
            SuccessRate = successRate,
            FailedPayments = failedPayments,
            Timeline = timeline,
            AverageProcessingTime = averageProcessingTime
        };

        // Cache for 10 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(statistics), cacheOptions, cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Payment statistics retrieved successfully", statistics);
    }

    private List<PaymentTimelineItem> GroupPaymentsByInterval(
        List<Domain.Entities.SubscriptionPayment> payments,
        string interval)
    {
        var timeline = new List<PaymentTimelineItem>();

        IEnumerable<IGrouping<DateTime, Domain.Entities.SubscriptionPayment>> grouped;

        if (interval == "day")
        {
            grouped = payments.GroupBy(p => p.CreatedAt.Date).OrderBy(g => g.Key);
        }
        else if (interval == "week")
        {
            grouped = payments.GroupBy(p => GetWeekStart(p.CreatedAt)).OrderBy(g => g.Key);
        }
        else // month
        {
            grouped = payments.GroupBy(p => new DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1)).OrderBy(g => g.Key);
        }

        foreach (var group in grouped)
        {
            timeline.Add(new PaymentTimelineItem
            {
                Date = group.Key,
                TotalCount = group.Count(),
                SuccessCount = group.Count(p => p.Status == SubscriptionPaymentStatus.Paid),
                FailedCount = group.Count(p => p.Status == SubscriptionPaymentStatus.Failed)
            });
        }

        return timeline;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
