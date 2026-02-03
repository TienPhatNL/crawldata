using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionTiers.DTOs;
using SubscriptionTierEntity = UserService.Domain.Entities.SubscriptionTier;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionTiers.Commands;

public class UpdateSubscriptionTierCommand : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
    public UpdateSubscriptionTierDto Tier { get; set; } = null!;
}

public class UpdateSubscriptionTierCommandHandler : IRequestHandler<UpdateSubscriptionTierCommand, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly ILogger<UpdateSubscriptionTierCommandHandler> _logger;

    public UpdateSubscriptionTierCommandHandler(
        UserDbContext context,
        ILogger<UpdateSubscriptionTierCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(UpdateSubscriptionTierCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tier = await _context.SubscriptionTiers
                .Include(t => t.SubscriptionPlans)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (tier == null)
            {
                return new ResponseModel(HttpStatusCode.NotFound, "Subscription tier not found");
            }

            // Validate: Cannot set tier to inactive if it has active subscription plans
            if (tier.IsActive && !request.Tier.IsActive)
            {
                var hasActiveSubscriptionPlans = tier.SubscriptionPlans.Any(p => !p.IsDeleted && p.IsActive);
                if (hasActiveSubscriptionPlans)
                {
                    return new ResponseModel(HttpStatusCode.BadRequest, 
                        "Cannot set tier to inactive while it has active subscription plans. Please deactivate or delete all subscription plans first.");
                }
            }

            // Check if level conflicts with another tier
            if (tier.Level != request.Tier.Level)
            {
                var conflictingTier = await _context.SubscriptionTiers
                    .FirstOrDefaultAsync(t => t.Level == request.Tier.Level && t.Id != request.Id, 
                        cancellationToken);

                if (conflictingTier != null)
                {
                    return new ResponseModel(HttpStatusCode.BadRequest, 
                        $"A tier with level {request.Tier.Level} already exists");
                }
            }

            tier.Name = request.Tier.Name;
            tier.Description = request.Tier.Description;
            tier.Level = request.Tier.Level;
            tier.IsActive = request.Tier.IsActive;
            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated subscription tier {TierId}", tier.Id);

            var dto = new SubscriptionTierDto
            {
                Id = tier.Id,
                Name = tier.Name,
                Description = tier.Description,
                Level = tier.Level,
                IsActive = tier.IsActive,
                CreatedAt = tier.CreatedAt,
                UpdatedAt = tier.UpdatedAt
            };

            return new ResponseModel(HttpStatusCode.OK, "Subscription tier updated successfully", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription tier {TierId}", request.Id);
            return new ResponseModel(HttpStatusCode.InternalServerError, "An error occurred while updating the tier");
        }
    }
}
