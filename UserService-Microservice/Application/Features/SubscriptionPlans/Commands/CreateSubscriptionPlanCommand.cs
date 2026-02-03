using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Application.Features.SubscriptionPlans.DTOs;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionPlans.Commands;

public class CreateSubscriptionPlanCommand : IRequest<ResponseModel>
{
    public CreateSubscriptionPlanDto Plan { get; set; } = null!;
}

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, ResponseModel>
{
    private readonly UserDbContext _context;

    public CreateSubscriptionPlanCommandHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
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

        // Note: Removed constraint - now a tier can have many plans (including multiple active plans)

        var entity = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Plan.Name,
            Description = request.Plan.Description,
            Price = request.Plan.Price,
            Currency = request.Plan.Currency,
            DurationDays = request.Plan.DurationDays,
            QuotaLimit = request.Plan.QuotaLimit,
            Features = request.Plan.Features,
            IsActive = request.Plan.IsActive,
            SubscriptionTierId = request.Plan.SubscriptionTierId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionPlans.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new ResponseModel(HttpStatusCode.Created, "Subscription plan created successfully", entity.Id);
    }
}
