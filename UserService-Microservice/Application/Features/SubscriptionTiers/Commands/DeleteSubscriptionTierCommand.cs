using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using SubscriptionTierEntity = UserService.Domain.Entities.SubscriptionTier;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionTiers.Commands;

public class DeleteSubscriptionTierCommand : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
}

public class DeleteSubscriptionTierCommandHandler : IRequestHandler<DeleteSubscriptionTierCommand, ResponseModel>
{
    private readonly UserDbContext _context;
    private readonly ILogger<DeleteSubscriptionTierCommandHandler> _logger;

    public DeleteSubscriptionTierCommandHandler(
        UserDbContext context,
        ILogger<DeleteSubscriptionTierCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(DeleteSubscriptionTierCommand request, CancellationToken cancellationToken)
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

            // Check if tier has active subscription plans
            if (tier.SubscriptionPlans.Any(p => !p.IsDeleted))
            {
                return new ResponseModel(HttpStatusCode.BadRequest, 
                    "Cannot delete tier with active subscription plans");
            }

            // Soft delete
            tier.IsDeleted = true;
            tier.DeletedAt = DateTime.UtcNow;
            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Soft deleted subscription tier {TierId}", tier.Id);

            return new ResponseModel(HttpStatusCode.OK, "Subscription tier deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription tier {TierId}", request.Id);
            return new ResponseModel(HttpStatusCode.InternalServerError, "An error occurred while deleting the tier");
        }
    }
}
