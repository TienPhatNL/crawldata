using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Domain.Interfaces;
using UserService.Domain.Services;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Subscriptions.Commands;

public class UpgradeSubscriptionCommandHandler : IRequestHandler<UpgradeSubscriptionCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpgradeSubscriptionCommandHandler> _logger;
    private readonly IQuotaSnapshotService _quotaSnapshotService;
    private readonly IDistributedCache _cache;
    private readonly IWebCrawlerQuotaCacheWriter _webCrawlerQuotaCacheWriter;

    public UpgradeSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpgradeSubscriptionCommandHandler> logger,
        IQuotaSnapshotService quotaSnapshotService,
        IDistributedCache cache,
        IWebCrawlerQuotaCacheWriter webCrawlerQuotaCacheWriter)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _quotaSnapshotService = quotaSnapshotService;
        _cache = cache;
        _webCrawlerQuotaCacheWriter = webCrawlerQuotaCacheWriter;
    }

    public async Task<ResponseModel> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Subscription upgrade attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Fetch the new subscription plan
        var newPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(request.SubscriptionPlanId, cancellationToken);
        if (newPlan == null || newPlan.IsDeleted || !newPlan.IsActive)
        {
            _logger.LogWarning("Invalid or inactive subscription plan {PlanId} requested for user {UserId}", request.SubscriptionPlanId, request.UserId);
            throw new ValidationException("Subscription plan not found or unavailable");
        }

        var oldTier = user.SubscriptionTier;
        var newTier = newPlan.Tier;
        
        if (request.IsRenewal && oldTier?.Id == newTier?.Id)
        {
            return await RenewExistingSubscriptionAsync(user, newPlan, request, cancellationToken);
        }

        if (!IsValidUpgrade(oldTier, newTier))
        {
            _logger.LogWarning("Invalid subscription upgrade from {OldTier} to {NewTier} for user {UserId}",
                oldTier?.Name, newTier?.Name, request.UserId);
            throw new ValidationException($"Invalid subscription upgrade from {oldTier?.Name} to {newTier?.Name}");
        }

        return await UpgradeToNewTierAsync(user, oldTier, newPlan, request, cancellationToken);
    }

    private async Task<ResponseModel> UpgradeToNewTierAsync(
        User user,
        Domain.Entities.SubscriptionTier? oldTier,
        SubscriptionPlan newPlan,
        UpgradeSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var newQuotaLimit = request.CustomQuotaLimit ?? newPlan.QuotaLimit;
        var now = DateTime.UtcNow;
        var currentSubscription = await GetSubscriptionForUserAsync(user, cancellationToken);
        var isNewSubscription = currentSubscription == null;
        currentSubscription ??= new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = now
        };

        var subscriptionStartDate = now;
        var subscriptionEndDate = CalculateSubscriptionEndDate(newPlan, subscriptionStartDate);

        var response = await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            currentSubscription.SubscriptionPlanId = newPlan.Id;
            currentSubscription.StartDate = subscriptionStartDate;
            currentSubscription.EndDate = subscriptionEndDate;
            currentSubscription.IsActive = true;
            currentSubscription.QuotaLimit = newQuotaLimit;
            currentSubscription.Price = newPlan.Price;
            currentSubscription.Currency = newPlan.Currency;
            currentSubscription.PaymentReference = request.PaymentReference ?? currentSubscription.PaymentReference;
            currentSubscription.CancelledAt = null;
            currentSubscription.CancellationReason = null;
            currentSubscription.UpdatedAt = now;

            if (isNewSubscription)
            {
                await _unitOfWork.UserSubscriptions.AddAsync(currentSubscription, cancellationToken);
            }
            else
            {
                await _unitOfWork.UserSubscriptions.UpdateAsync(currentSubscription, cancellationToken);
            }

            user.CurrentSubscriptionId = currentSubscription.Id;
            user.CurrentSubscriptionPlanId = newPlan.Id;
            user.SubscriptionStartDate = subscriptionStartDate;
            user.SubscriptionEndDate = subscriptionEndDate;
            user.CrawlQuotaLimit = newQuotaLimit;
            user.UpdatedAt = now;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            await _quotaSnapshotService.UpsertFromUserAsync(
                user,
                source: "subscription-upgrade",
                isOverride: request.CustomQuotaLimit.HasValue,
                synchronizedAt: DateTime.UtcNow,
                cancellationToken);

            user.AddDomainEvent(new UserSubscriptionUpgradedEvent(user.Id, user.Email, oldTier, newPlan.Tier, newQuotaLimit));

            _logger.LogInformation("Subscription upgraded for user {UserId}: {OldTier} -> {NewTier}, QuotaLimit: {QuotaLimit}",
                user.Id, oldTier?.Name, newPlan.Tier?.Name, newQuotaLimit);

            var data = new
            {
                oldTier = oldTier?.Name ?? "Free",
                newTier = newPlan.Tier?.Name ?? "Unknown",
                planName = newPlan.Name,
                subscriptionStartDate = subscriptionStartDate,
                subscriptionEndDate = subscriptionEndDate,
                newQuotaLimit = newQuotaLimit
            };

            await InvalidateDashboardCachesAsync(cancellationToken);
            return new ResponseModel(HttpStatusCode.OK, "Subscription upgraded successfully", data);
        }, cancellationToken);

        await SeedWebCrawlerQuotaCacheAsync(user, newPlan.Tier?.Name ?? "Unknown", cancellationToken);
        return response;
    }

    private async Task<ResponseModel> RenewExistingSubscriptionAsync(
        User user,
        SubscriptionPlan newPlan,
        UpgradeSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var currentSubscription = await GetSubscriptionForUserAsync(user, cancellationToken);
        if (currentSubscription == null)
        {
            _logger.LogWarning("Renewal requested but subscription record missing for user {UserId}", user.Id);
            request.IsRenewal = false;
            return await UpgradeToNewTierAsync(user, user.SubscriptionTier, newPlan, request, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var newQuotaLimit = request.CustomQuotaLimit ?? newPlan.QuotaLimit;
        var baseDate = currentSubscription.EndDate.HasValue && currentSubscription.EndDate.Value > now
            ? currentSubscription.EndDate.Value
            : now;
        var newEndDate = CalculateSubscriptionEndDate(newPlan, baseDate);

        var response = await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            currentSubscription.SubscriptionPlanId = newPlan.Id;
            currentSubscription.EndDate = newEndDate;
            currentSubscription.IsActive = true;
            currentSubscription.PaymentReference = request.PaymentReference ?? currentSubscription.PaymentReference;
            currentSubscription.QuotaLimit = newQuotaLimit;
            currentSubscription.Price = newPlan.Price;
            currentSubscription.Currency = newPlan.Currency;
            currentSubscription.CancelledAt = null;
            currentSubscription.CancellationReason = null;
            currentSubscription.UpdatedAt = now;

            await _unitOfWork.UserSubscriptions.UpdateAsync(currentSubscription, cancellationToken);

            user.CurrentSubscriptionId = currentSubscription.Id;
            user.CurrentSubscriptionPlanId = newPlan.Id;
            user.SubscriptionEndDate = newEndDate;
            user.CrawlQuotaLimit = newQuotaLimit;
            user.UpdatedAt = now;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            await _quotaSnapshotService.UpsertFromUserAsync(
                user,
                source: "subscription-renewal",
                isOverride: request.CustomQuotaLimit.HasValue,
                synchronizedAt: DateTime.UtcNow,
                cancellationToken);

            _logger.LogInformation("Subscription renewed for user {UserId}: tier {Tier} now valid until {EndDate}",
                user.Id, newPlan.Tier, newEndDate);

            var data = new
            {
                tier = newPlan.Tier.ToString(),
                planName = newPlan.Name,
                subscriptionStartDate = user.SubscriptionStartDate,
                subscriptionEndDate = newEndDate,
                newQuotaLimit = newQuotaLimit
            };

            await InvalidateDashboardCachesAsync(cancellationToken);
            return new ResponseModel(HttpStatusCode.OK, "Subscription renewed successfully", data);
        }, cancellationToken);

        await SeedWebCrawlerQuotaCacheAsync(user, newPlan.Tier.ToString(), cancellationToken);
        return response;
    }

    private async Task<UserSubscription?> GetSubscriptionForUserAsync(User user, CancellationToken cancellationToken)
    {
        if (user.CurrentSubscriptionId.HasValue)
        {
            var byId = await _unitOfWork.UserSubscriptions.GetByIdAsync(user.CurrentSubscriptionId.Value, cancellationToken);
            if (byId != null)
            {
                return byId;
            }
        }

        return await _unitOfWork.UserSubscriptions.GetAsync(s => s.UserId == user.Id, cancellationToken);
    }

    private async Task InvalidateDashboardCachesAsync(CancellationToken cancellationToken)
    {
        var dateRanges = new[]
        {
            (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, "7days"),
            (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, "30days"),
            (DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, "3months"),
            (DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, "6months"),
            (DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, "1year"),
            (DateTime.UtcNow.AddYears(-2), DateTime.UtcNow, "2years")
        };

        var quotaThresholds = new[] { 50, 60, 70, 75, 80, 85, 90 };
        var intervals = new[] { "day", "week", "month" };

        var tasks = new List<Task>();

        foreach (var (startDate, endDate, label) in dateRanges)
        {
            var startKey = startDate.ToString("yyyy-MM-dd");
            var endKey = endDate.ToString("yyyy-MM-dd");

            // User statistics cache keys (with quota threshold and interval variations)
            foreach (var threshold in quotaThresholds)
            {
                foreach (var interval in intervals)
                {
                    var userKey = $"admin:dashboard:users:{startKey}:{endKey}:{threshold}:{interval}";
                    tasks.Add(_cache.RemoveAsync(userKey, cancellationToken));
                }
            }

            // Subscription statistics cache keys (with interval variations)
            foreach (var interval in intervals)
            {
                var subKey = $"admin:dashboard:subscriptions:{startKey}:{endKey}:{interval}";
                tasks.Add(_cache.RemoveAsync(subKey, cancellationToken));
            }
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Successfully invalidated {Count} dashboard cache keys after subscription upgrade", tasks.Count);
    }

    private async Task SeedWebCrawlerQuotaCacheAsync(User user, string planType, CancellationToken cancellationToken)
    {
        var quotaLimit = Math.Max(0, user.CrawlQuotaLimit);
        var quotaUsed = Math.Max(0, user.CrawlQuotaUsed);
        var remaining = Math.Max(0, quotaLimit - quotaUsed);

        try
        {
            await _webCrawlerQuotaCacheWriter.SetQuotaAsync(
                user.Id,
                remaining,
                quotaLimit,
                planType,
                user.QuotaResetDate,
                DateTime.UtcNow,
                TimeSpan.FromMinutes(60),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed WebCrawler quota cache for user {UserId}", user.Id);
        }
    }

    private static bool IsValidUpgrade(Domain.Entities.SubscriptionTier? currentTier, Domain.Entities.SubscriptionTier? newTier)
    {
        if (currentTier == null || newTier == null) return false;
        
        // Free users can upgrade to any paid tier
        if (currentTier.Level == 0)
            return newTier.Level > 0;

        // Basic users can upgrade to Premium or Enterprise
        if (currentTier.Level == 1)
            return newTier.Level == 2 || newTier.Level == 3;

        // Premium users can upgrade to Enterprise
        if (currentTier.Level == 2)
            return newTier.Level == 3;

        // Enterprise users can't upgrade further
        return false;
    }

    private static DateTime CalculateSubscriptionEndDate(SubscriptionPlan plan, DateTime startDate)
    {
        if (plan.DurationDays == 0 || plan.Tier?.Level == 0)
        {
            return startDate.AddYears(100); // Free or perpetual plans never expire
        }

        return startDate.AddDays(plan.DurationDays);
    }
}
