using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.Payments.Queries;

public class GetSubscriptionPaymentsQuery : IRequest<ResponseModel>
{
    public Guid? UserId { get; set; }
    public bool IncludeAllUsers { get; set; }
    public Guid? TierId { get; set; } // Changed from SubscriptionTier enum to Guid
    public SubscriptionPaymentStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetSubscriptionPaymentsQueryHandler : IRequestHandler<GetSubscriptionPaymentsQuery, ResponseModel>
{
    private readonly UserDbContext _dbContext;

    public GetSubscriptionPaymentsQueryHandler(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionPaymentsQuery request, CancellationToken cancellationToken)
    {
        if (!request.IncludeAllUsers && (!request.UserId.HasValue || request.UserId == Guid.Empty))
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "UserId is required for personal payment history.");
        }

        if (request.Page <= 0) request.Page = 1;
        if (request.PageSize <= 0) request.PageSize = 20;
        request.PageSize = Math.Min(request.PageSize, 100);

        var query = _dbContext.SubscriptionPayments.AsNoTracking().AsQueryable();

        if (!request.IncludeAllUsers && request.UserId.HasValue)
        {
            query = query.Where(p => p.UserId == request.UserId.Value);
            query = query.Include(p => p.SubscriptionPlan).ThenInclude(sp => sp.Tier);
        }
        else
        {
            if (request.UserId.HasValue && request.UserId != Guid.Empty)
            {
                query = query.Where(p => p.UserId == request.UserId.Value);
            }

            query = query.Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .ThenInclude(sp => sp.Tier);
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

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new SubscriptionPaymentDto
            {
                PaymentId = p.Id,
                OrderCode = p.OrderCode,
                TierId = p.SubscriptionPlan != null ? p.SubscriptionPlan.SubscriptionTierId : Guid.Empty,
                TierName = p.SubscriptionPlan != null && p.SubscriptionPlan.Tier != null ? p.SubscriptionPlan.Tier.Name : "Unknown",
                TierLevel = p.SubscriptionPlan != null && p.SubscriptionPlan.Tier != null ? p.SubscriptionPlan.Tier.Level : 0,
                Status = p.Status,
                Amount = p.Amount,
                Currency = p.Currency,
                CheckoutUrl = p.CheckoutUrl,
                CreatedAt = p.CreatedAt,
                ExpiredAt = p.ExpiredAt,
                PaidAt = p.PaidAt,
                CancelledAt = p.CancelledAt,
                FailureReason = p.FailureReason,
                PaymentReference = p.PaymentReference,
                UserId = p.UserId,
                UserEmail = p.User != null ? p.User.Email : null,
                UserFullName = p.User != null ? p.User.FullName : null
            })
            .ToListAsync(cancellationToken);

        var pageMetadata = new PagedSubscriptionPayments
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalCount,
            Items = items
        };

        return new ResponseModel(HttpStatusCode.OK, "Subscription payments retrieved", pageMetadata);
    }
    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }
}

public class PagedSubscriptionPayments
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public IReadOnlyCollection<SubscriptionPaymentDto> Items { get; set; } = Array.Empty<SubscriptionPaymentDto>();
}

public class SubscriptionPaymentDto
{
    public Guid PaymentId { get; set; }
    public string? OrderCode { get; set; }
    public Guid TierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int TierLevel { get; set; }
    public SubscriptionPaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? PaymentReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
}
