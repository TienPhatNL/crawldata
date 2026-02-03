using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Services;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.BackgroundServices;

/// <summary>
/// Periodically synchronizes per-user crawl quota limits with their active subscriptions
/// and records a snapshot that downstream services can query without hitting subscription tables.
/// </summary>
public class SubscriptionQuotaSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionQuotaSyncWorker> _logger;
    private readonly QuotaSyncSettings _syncSettings;
    private readonly RoleQuotaSettings _roleQuotaSettings;

    public SubscriptionQuotaSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<QuotaSyncSettings> syncOptions,
        IOptions<RoleQuotaSettings> roleQuotaOptions,
        ILogger<SubscriptionQuotaSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _syncSettings = syncOptions?.Value ?? new QuotaSyncSettings();
        _roleQuotaSettings = roleQuotaOptions?.Value ?? new RoleQuotaSettings();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short startup delay so migrations + Kafka consumer can finish wiring up first
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SynchronizeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quota synchronization cycle failed");
            }

            var delaySeconds = Math.Max(60, _syncSettings.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<IQuotaSnapshotService>();
        var now = DateTime.UtcNow;

        _logger.LogDebug("Running quota sync at {Timestamp}", now);

        var processedUsers = new HashSet<Guid>();

        var activeSubscriptions = await unitOfWork.UserSubscriptions.GetManyAsync(
            s => s.IsActive && (!s.EndDate.HasValue || s.EndDate.Value >= now),
            cancellationToken);

        foreach (var subscription in activeSubscriptions
                     .Where(s => s.UserId.HasValue)
                     .OrderByDescending(s => s.StartDate))
        {
            if (!subscription.UserId.HasValue)
            {
                continue;
            }

            var userId = subscription.UserId.Value;
            if (!processedUsers.Add(userId))
            {
                continue; // Already synced using the most recent subscription
            }

            await ApplyQuotaForUserAsync(
                unitOfWork,
                snapshotService,
                userId,
                subscription.SubscriptionPlanId,
                subscription.QuotaLimit,
                now,
                cancellationToken);
        }

        await ProcessRemainingUsersAsync(unitOfWork, snapshotService, processedUsers, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Quota sync completed for {Count} users", processedUsers.Count);
    }

    private async Task ProcessRemainingUsersAsync(
        IUnitOfWork unitOfWork,
        IQuotaSnapshotService snapshotService,
        HashSet<Guid> processedUsers,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var pageNumber = 1;
        var pageSize = Math.Max(50, _syncSettings.BatchSize);

        while (true)
        {
            var page = await unitOfWork.Users.GetPagedAsync(
                filter: u => !u.IsDeleted,
                orderBy: q => q.OrderBy(u => u.Id),
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            var users = page.Data.ToList();
            if (users.Count == 0)
            {
                break;
            }

            foreach (var user in users)
            {
                if (processedUsers.Contains(user.Id))
                {
                    continue;
                }

            await ApplyQuotaForUserAsync(
                unitOfWork,
                snapshotService,
                user.Id,
                user.CurrentSubscriptionPlanId,
                null,
                timestamp,
                cancellationToken,
                user);                processedUsers.Add(user.Id);
            }

            if (!page.HasNextPage)
            {
                break;
            }

            pageNumber++;
        }
    }

    private async Task ApplyQuotaForUserAsync(
        IUnitOfWork unitOfWork,
        IQuotaSnapshotService snapshotService,
        Guid userId,
        Guid? subscriptionPlanId,
        int? subscriptionQuota,
        DateTime timestamp,
        CancellationToken cancellationToken,
        User? existingUser = null)
    {
        var user = existingUser ?? await unitOfWork.Users.GetByIdWithPlanAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Skipping quota sync for missing user {UserId}", userId);
            return;
        }

        // Fetch plan details if we have a plan ID
        SubscriptionPlan? plan = null;
        if (subscriptionPlanId.HasValue)
        {
            plan = await unitOfWork.SubscriptionPlans.GetByIdAsync(subscriptionPlanId.Value, cancellationToken);
        }

        var targetLimit = ResolveLimitForPlan(user.Role, plan, subscriptionQuota);
        var nextReset = user.QuotaResetDate == default
            ? CalculateNextReset(timestamp)
            : user.QuotaResetDate;

        var userChanged = false;

        if (user.CrawlQuotaLimit != targetLimit)
        {
            user.CrawlQuotaLimit = targetLimit;
            userChanged = true;
        }

        if (user.CurrentSubscriptionPlanId != subscriptionPlanId)
        {
            user.CurrentSubscriptionPlanId = subscriptionPlanId;
            userChanged = true;
        }

        if (_syncSettings.EnableAutomaticReset && user.QuotaResetDate <= timestamp)
        {
            user.CrawlQuotaUsed = 0;
            nextReset = CalculateNextReset(timestamp);
            userChanged = true;
        }

        if (user.QuotaResetDate != nextReset)
        {
            user.QuotaResetDate = nextReset;
            userChanged = true;
        }

        if (userChanged)
        {
            await unitOfWork.Users.UpdateAsync(user, cancellationToken);
        }

        await snapshotService.UpsertFromUserAsync(
            user,
            subscriptionQuota.HasValue ? "subscription" : "role",
            subscriptionQuota.HasValue,
            timestamp,
            cancellationToken);
    }

    private int ResolveLimitForPlan(UserRole role, SubscriptionPlan? plan, int? subscriptionQuota)
    {
        if (subscriptionQuota.HasValue && subscriptionQuota.Value > 0)
        {
            return subscriptionQuota.Value;
        }

        // If no plan, use role-based defaults
        if (plan == null)
        {
            return role switch
            {
                UserRole.Student => _roleQuotaSettings.Student,
                UserRole.Lecturer => _roleQuotaSettings.Lecturer,
                UserRole.Staff => _roleQuotaSettings.Staff,
                UserRole.Admin => _roleQuotaSettings.Admin,
                _ => _roleQuotaSettings.Student
            };
        }

        // Use plan's quota limit
        return plan.QuotaLimit;
    }

    private DateTime CalculateNextReset(DateTime timestamp)
    {
        var days = Math.Max(1, _syncSettings.ResetWindowDays);
        return timestamp.AddDays(days);
    }
}
