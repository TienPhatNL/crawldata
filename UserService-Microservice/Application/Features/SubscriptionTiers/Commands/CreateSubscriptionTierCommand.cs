using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionTiers.DTOs;
using SubscriptionTierEntity = UserService.Domain.Entities.SubscriptionTier;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionTiers.Commands;

public class CreateSubscriptionTierCommand : IRequest<ResponseModel>
{
    public CreateSubscriptionTierDto Tier { get; set; } = null!;
}

public class CreateSubscriptionTierCommandHandler : IRequestHandler<CreateSubscriptionTierCommand, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly ILogger<CreateSubscriptionTierCommandHandler> _logger;

    public CreateSubscriptionTierCommandHandler(
        UserDbContext context,
        ILogger<CreateSubscriptionTierCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(CreateSubscriptionTierCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if tier with same level already exists
            var existingTier = await _context.SubscriptionTiers
                .FirstOrDefaultAsync(t => t.Level == request.Tier.Level, cancellationToken);

            if (existingTier != null)
            {
                return new ResponseModel(HttpStatusCode.BadRequest, 
                    $"A tier with level {request.Tier.Level} already exists");
            }

            var tier = new SubscriptionTierEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Tier.Name,
                Description = request.Tier.Description,
                Level = request.Tier.Level,
                IsActive = request.Tier.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SubscriptionTiers.Add(tier);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created subscription tier {TierId} with name {TierName}", tier.Id, tier.Name);

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

            return new ResponseModel(HttpStatusCode.Created, "Subscription tier created successfully", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription tier");
            return new ResponseModel(HttpStatusCode.InternalServerError, "An error occurred while creating the tier");
        }
    }
}
