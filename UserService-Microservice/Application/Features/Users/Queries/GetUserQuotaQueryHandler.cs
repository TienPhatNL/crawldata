using System;
using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Queries;

public class GetUserQuotaQueryHandler : IRequestHandler<GetUserQuotaQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserQuotaQueryHandler> _logger;

    public GetUserQuotaQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserQuotaQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetUserQuotaQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Quota requested for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var snapshot = await _unitOfWork.UserQuotaSnapshots.GetAsync(
            q => q.UserId == request.UserId,
            cancellationToken);

        var quotaLimit = snapshot?.QuotaLimit ?? user.CrawlQuotaLimit;
        var quotaUsed = snapshot?.QuotaUsed ?? user.CrawlQuotaUsed;
        var quotaReset = snapshot?.QuotaResetDate ?? user.QuotaResetDate;
        var remaining = Math.Max(0, quotaLimit - quotaUsed);

        var payload = new
        {
            userId = user.Id,
            role = user.Role.ToString(),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free",
            quotaLimit,
            quotaUsed,
            quotaRemaining = remaining,
            quotaResetDate = quotaReset,
            lastSynchronizedAt = snapshot?.LastSynchronizedAt ?? user.UpdatedAt ?? user.CreatedAt,
            source = snapshot?.Source ?? "user",
            isOverride = snapshot?.IsOverride ?? false,
            subscriptionStartDate = user.SubscriptionStartDate,
            subscriptionEndDate = user.SubscriptionEndDate
        };

        _logger.LogDebug("Quota information returned for user {UserId}", user.Id);

        return new ResponseModel(HttpStatusCode.OK, "Quota information retrieved successfully", payload);
    }
}
