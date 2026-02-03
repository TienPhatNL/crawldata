using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionPlans.DTOs;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionPlans.Queries;

public class GetSubscriptionPlansQuery : IRequest<ResponseModel>
{
    public bool? IsActive { get; set; }
}

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, ResponseModel>
{
    private readonly UserDbContext _context;

    public GetSubscriptionPlansQueryHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SubscriptionPlans.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var plans = await query
            .Include(p => p.Tier)
            .OrderBy(p => p.Price)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                QuotaLimit = p.QuotaLimit,
                Features = p.Features,
                IsActive = p.IsActive,
                SubscriptionTierId = p.SubscriptionTierId,
                TierName = p.Tier.Name,
                TierLevel = p.Tier.Level,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Subscription plans retrieved successfully", plans);
    }
}
