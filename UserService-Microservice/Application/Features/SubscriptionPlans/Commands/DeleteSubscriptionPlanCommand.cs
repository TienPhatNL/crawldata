using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Persistence;

namespace UserService.Application.Features.SubscriptionPlans.Commands;

public class DeleteSubscriptionPlanCommand : IRequest<ResponseModel>
{
    public Guid Id { get; set; }
}

public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand, ResponseModel>
{
    private readonly UserDbContext _context;

    public DeleteSubscriptionPlanCommandHandler(UserDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return new ResponseModel(HttpStatusCode.NotFound, "Subscription plan not found");
        }

        // Toggle IsActive status
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var status = entity.IsActive ? "activated" : "deactivated";
        return new ResponseModel(HttpStatusCode.OK, $"Subscription plan {status} successfully", new { Id = entity.Id, IsActive = entity.IsActive });
    }
}
