using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionPlans.DTOs;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionPlans.Queries;

public class GetSubscriptionPlanByIdQuery : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
}

public class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, ResponseModel>
{
    private readonly UserDbContext _context;

    public GetSubscriptionPlanByIdQueryHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _context.SubscriptionPlans
            .Include(p => p.Tier)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (plan == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Subscription plan not found");
        }

        var dto = new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            QuotaLimit = plan.QuotaLimit,
            Features = plan.Features,
            IsActive = plan.IsActive,
            SubscriptionTierId = plan.SubscriptionTierId,
            TierName = plan.Tier?.Name,
            TierLevel = plan.Tier?.Level,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };

        return new ResponseModel(HttpStatusCode.OK, "Subscription plan retrieved successfully", dto);
    }
}
