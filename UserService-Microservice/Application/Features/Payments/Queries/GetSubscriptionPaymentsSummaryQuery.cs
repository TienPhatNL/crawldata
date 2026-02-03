using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.Payments.Queries;

public class GetSubscriptionPaymentsSummaryQuery : IRequest<ResponseModel>
{
    public Guid? UserId { get; set; }
    public Guid? TierId { get; set; } // Changed from SubscriptionTier enum to Guid
    public SubscriptionPaymentStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class GetSubscriptionPaymentsSummaryQueryHandler : IRequestHandler<GetSubscriptionPaymentsSummaryQuery, ResponseModel>
{
    private readonly UserDbContext _dbContext;

    public GetSubscriptionPaymentsSummaryQueryHandler(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionPaymentsSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.SubscriptionPayments
            .Include(p => p.SubscriptionPlan)
            .ThenInclude(sp => sp.Tier)
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue && request.UserId != Guid.Empty)
        {
            query = query.Where(p => p.UserId == request.UserId.Value);
        }

        if (request.TierId.HasValue)
        {
            query = query.Where(p => p.SubscriptionPlan != null && p.SubscriptionPlan.SubscriptionTierId == request.TierId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.From.HasValue)
        {
            var from = NormalizeUtc(request.From.Value);
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (request.To.HasValue)
        {
            var to = NormalizeUtc(request.To.Value);
            query = query.Where(p => p.CreatedAt <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var grouped = await query
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var paidAmount = await query
            .Where(p => p.Status == SubscriptionPaymentStatus.Paid)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var breakdown = Enum.GetValues<SubscriptionPaymentStatus>()
            .Cast<SubscriptionPaymentStatus>()
            .ToDictionary(status => status, _ => 0);

        foreach (var entry in grouped)
        {
            breakdown[entry.Status] = entry.Count;
        }

        var summary = new SubscriptionPaymentsSummary
        {
            TotalPayments = totalCount,
            TotalRevenue = paidAmount,
            StatusBreakdown = breakdown
        };

        return new ResponseModel(HttpStatusCode.OK, "Subscription payment summary", summary);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }
}

public class SubscriptionPaymentsSummary
{
    public int TotalPayments { get; set; }
    public decimal TotalRevenue { get; set; }
    public IDictionary<SubscriptionPaymentStatus, int> StatusBreakdown { get; set; } = new Dictionary<SubscriptionPaymentStatus, int>();
}
