using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Services;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Services;

public class QuotaSnapshotService : IQuotaSnapshotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuotaSnapshotService> _logger;

    public QuotaSnapshotService(IUnitOfWork unitOfWork, ILogger<QuotaSnapshotService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserQuotaSnapshot> UpsertFromUserAsync(
        User user,
        string source,
        bool isOverride,
        DateTime? synchronizedAt,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _unitOfWork.UserQuotaSnapshots.GetAsync(
            q => q.UserId == user.Id,
            cancellationToken);

        if (snapshot == null)
        {
            snapshot = new UserQuotaSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SubscriptionPlanId = user.CurrentSubscriptionPlanId,
                QuotaLimit = user.CrawlQuotaLimit,
                QuotaUsed = user.CrawlQuotaUsed,
                QuotaResetDate = user.QuotaResetDate,
                LastSynchronizedAt = synchronizedAt ?? DateTime.UtcNow,
                Source = source,
                IsOverride = isOverride
            };

            await _unitOfWork.UserQuotaSnapshots.AddAsync(snapshot, cancellationToken);
            _logger.LogDebug("Created quota snapshot for user {UserId} via {Source}", user.Id, source);
        }
        else
        {
            snapshot.SubscriptionPlanId = user.CurrentSubscriptionPlanId;
            snapshot.QuotaLimit = user.CrawlQuotaLimit;
            snapshot.QuotaUsed = user.CrawlQuotaUsed;
            snapshot.QuotaResetDate = user.QuotaResetDate;
            snapshot.LastSynchronizedAt = synchronizedAt ?? DateTime.UtcNow;
            snapshot.Source = source;
            snapshot.IsOverride = isOverride;

            await _unitOfWork.UserQuotaSnapshots.UpdateAsync(snapshot, cancellationToken);
            _logger.LogDebug("Updated quota snapshot for user {UserId} via {Source}", user.Id, source);
        }

        return snapshot;
    }
}
