using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Domain.Enums;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.BackgroundServices;

/// <summary>
/// Periodically processes expired subscriptions and payments.
/// - Deactivates subscriptions past their EndDate
/// - Updates pending payments past their ExpiresAt date to Expired status
/// </summary>
public class ExpiredSubscriptionSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredSubscriptionSyncBackgroundService> _logger;
    private readonly SubscriptionExpirySyncSettings _settings;

    public ExpiredSubscriptionSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredSubscriptionSyncBackgroundService> logger,
        IOptions<SubscriptionExpirySyncSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings?.Value ?? new SubscriptionExpirySyncSettings();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Subscription Sync Background Service started");

        // Short startup delay for migrations and infrastructure setup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncExpiredSubscriptionsAndPaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Expired Subscription Sync Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Expired Subscription Sync Background Service");
                // Wait 5 minutes before retry on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            var delaySeconds = Math.Max(60, _settings.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task SyncExpiredSubscriptionsAndPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;
        _logger.LogDebug("Running expired subscription/payment sync at {Timestamp}", now);

        // Process expired subscriptions
        await ProcessExpiredSubscriptionsAsync(unitOfWork, mediator, now, cancellationToken);

        // Process expired payments
        await ProcessExpiredPaymentsAsync(unitOfWork, now, cancellationToken);
    }

    private async Task ProcessExpiredSubscriptionsAsync(
        IUnitOfWork unitOfWork,
        IMediator mediator,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var expiredSubscriptions = await unitOfWork.UserSubscriptions
            .GetManyAsync(
                s => s.IsActive && s.EndDate.HasValue && s.EndDate.Value < now,
                cancellationToken);

        var subscriptionsToProcess = expiredSubscriptions.Take(_settings.BatchSize).ToList();

        if (subscriptionsToProcess.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} expired subscriptions", subscriptionsToProcess.Count);

        foreach (var subscription in subscriptionsToProcess)
        {
            if (!subscription.UserId.HasValue)
            {
                continue;
            }

            try
            {
                // Deactivate subscription directly
                subscription.IsActive = false;
                subscription.CancellationReason = $"Subscription expired on {subscription.EndDate:yyyy-MM-dd HH:mm:ss} UTC";
                subscription.CancelledAt = now;
                subscription.UpdatedAt = now;

                await unitOfWork.UserSubscriptions.UpdateAsync(subscription, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cancelled expired subscription {SubscriptionId} for user {UserId}, ended {EndDate}",
                    subscription.Id,
                    subscription.UserId.Value,
                    subscription.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error cancelling expired subscription {SubscriptionId} for user {UserId}", 
                    subscription.Id,
                    subscription.UserId);
            }
        }
    }

    private async Task ProcessExpiredPaymentsAsync(
        IUnitOfWork unitOfWork,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var expiredPayments = await unitOfWork.SubscriptionPayments
            .GetManyAsync(
                p => p.Status == SubscriptionPaymentStatus.Pending && 
                     p.ExpiredAt.HasValue && 
                     p.ExpiredAt.Value < now,
                cancellationToken);

        var paymentsToProcess = expiredPayments.Take(_settings.BatchSize).ToList();

        if (paymentsToProcess.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} expired payments", paymentsToProcess.Count);

        foreach (var payment in paymentsToProcess)
        {
            try
            {
                payment.Status = SubscriptionPaymentStatus.Expired;
                payment.FailureReason = $"Payment expired at {payment.ExpiredAt:yyyy-MM-dd HH:mm:ss} UTC";
                payment.UpdatedAt = now;

                await unitOfWork.SubscriptionPayments.UpdateAsync(payment, cancellationToken);

                _logger.LogInformation(
                    "Expired payment {OrderCode} for user {UserId}",
                    payment.OrderCode,
                    payment.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error updating expired payment {PaymentId} (OrderCode: {OrderCode})", 
                    payment.Id,
                    payment.OrderCode);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated {Count} expired payments", paymentsToProcess.Count);
    }
}
