using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionTiers.DTOs;
using SubscriptionTierEntity = UserService.Domain.Entities.SubscriptionTier;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionTiers.Queries;

public class GetSubscriptionTierByIdQuery : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
}

public class GetSubscriptionTierByIdQueryHandler : IRequestHandler<GetSubscriptionTierByIdQuery, ResponseModel>
{
    private readonly UserDbContext _context;

    public GetSubscriptionTierByIdQueryHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(GetSubscriptionTierByIdQuery request, CancellationToken cancellationToken)
    {
        var tier = await _context.SubscriptionTiers
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (tier == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Subscription tier not found");
        }

        return new ResponseModel(HttpStatusCode.OK, "Subscription tier retrieved successfully", tier);
    }
}
