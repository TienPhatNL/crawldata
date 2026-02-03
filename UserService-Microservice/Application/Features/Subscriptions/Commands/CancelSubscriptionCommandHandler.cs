using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Domain.Services;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;
    private readonly IQuotaSnapshotService _quotaSnapshotService;

    public CancelSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CancelSubscriptionCommandHandler> logger,
        IQuotaSnapshotService quotaSnapshotService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _quotaSnapshotService = quotaSnapshotService;
    }

    public async Task<ResponseModel> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Subscription cancellation attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Check if user has an active subscription
        if (user.SubscriptionTier?.Level == 0)
        {
            _logger.LogWarning("Subscription cancellation attempted for user {UserId} who already has free subscription", request.UserId);
            throw new ValidationException("User already has a free subscription");
        }

        var previousTier = user.SubscriptionTier;
        var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow;

        // Fetch the Free plan from database
        var freePlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var freePlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(freePlanId, cancellationToken);
        if (freePlan == null)
        {
            _logger.LogError("Free subscription plan not found in database");
            throw new ValidationException("Free subscription plan not configured");
        }

        // Deactivate current subscription
        if (user.CurrentSubscriptionId.HasValue)
        {
            var currentSubscription = await _unitOfWork.UserSubscriptions.GetByIdAsync(user.CurrentSubscriptionId.Value, cancellationToken);
            if (currentSubscription != null)
            {
                currentSubscription.IsActive = false;
                currentSubscription.EndDate = effectiveDate;
                currentSubscription.CancellationReason = request.Reason;
                currentSubscription.CancelledAt = DateTime.UtcNow;
                currentSubscription.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserSubscriptions.UpdateAsync(currentSubscription, cancellationToken);
            }
        }

        // Downgrade to free tier
        var newQuotaLimit = freePlan.QuotaLimit;
        user.CurrentSubscriptionId = null;
        user.CurrentSubscriptionPlanId = freePlan.Id;
        user.SubscriptionStartDate = effectiveDate;
        user.SubscriptionEndDate = null; // Free tier doesn't expire
        user.CrawlQuotaLimit = newQuotaLimit;
        user.UpdatedAt = DateTime.UtcNow;

        // If user has used more quota than free tier allows, reset to limit
        if (user.CrawlQuotaUsed > newQuotaLimit)
        {
            user.CrawlQuotaUsed = newQuotaLimit;
        }

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        await _quotaSnapshotService.UpsertFromUserAsync(
            user,
            source: "subscription-cancelled",
            isOverride: false,
            synchronizedAt: DateTime.UtcNow,
            cancellationToken);

        // Add domain event
        user.AddDomainEvent(new UserSubscriptionCancelledEvent(user.Id, user.Email, previousTier, request.Reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription cancelled for user {UserId}: {PreviousTier} -> Free, effective {EffectiveDate}",
            user.Id, previousTier?.Name, effectiveDate);

        var data = new
        {
            effectiveDate = effectiveDate,
            previousTier = previousTier?.Name ?? "Unknown",
            newQuotaLimit = newQuotaLimit
        };

        return new ResponseModel(HttpStatusCode.OK, "Subscription cancelled successfully", data);
    }
}