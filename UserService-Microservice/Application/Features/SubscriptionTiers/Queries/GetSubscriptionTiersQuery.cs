using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionTiers.DTOs;
using SubscriptionTierEntity = UserService.Domain.Entities.SubscriptionTier;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionTiers.Queries;

public class GetSubscriptionTiersQuery : IRequest<ResponseModel>
{
    public bool? IsActive { get; set; }
}

public class GetSubscriptionTiersQueryHandler : IRequestHandler<GetSubscriptionTiersQuery, ResponseModel>
{
    private readonly UserDbContext _context;

    public GetSubscriptionTiersQueryHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionTiersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SubscriptionTiers.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        var tiers = await query
            .OrderBy(t => t.Level)
            .Select(t => new SubscriptionTierDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Level = t.Level,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Subscription tiers retrieved successfully", tiers);
    }
}
