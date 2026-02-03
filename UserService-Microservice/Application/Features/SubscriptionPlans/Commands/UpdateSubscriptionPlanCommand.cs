using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionPlans.DTOs;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionPlans.Commands;

public class UpdateSubscriptionPlanCommand : IRequest<ResponseModel>
{
    public UpdateSubscriptionPlanDto Plan { get; set; } = null!;
}

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, ResponseModel>
{
    private readonly UserDbContext _context;

    public UpdateSubscriptionPlanCommandHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        // Validation
        if (request.Plan.Price < 0)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Price cannot be negative");
        }

        if (request.Plan.QuotaLimit < 0)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Quota limit cannot be negative");
        }

        if (request.Plan.DurationDays < 0)
        {
            return new ResponseModel(HttpStatusCode.BadRequest, "Duration days cannot be negative");
        }

        var entity = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.Plan.Id, cancellationToken);

        if (entity == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Subscription plan not found");
        }

        entity.Name = request.Plan.Name;
        entity.Description = request.Plan.Description;
        entity.Price = request.Plan.Price;
        entity.Currency = request.Plan.Currency;
        entity.DurationDays = request.Plan.DurationDays;
        entity.QuotaLimit = request.Plan.QuotaLimit;
        entity.Features = request.Plan.Features;
        entity.IsActive = request.Plan.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResponseModel(HttpStatusCode.OK, "Subscription plan updated successfully", entity.Id);
    }
}
