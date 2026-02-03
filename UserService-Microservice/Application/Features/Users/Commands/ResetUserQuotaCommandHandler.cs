using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Commands;

public class ResetUserQuotaCommandHandler : IRequestHandler<ResetUserQuotaCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetUserQuotaCommandHandler> _logger;

    public ResetUserQuotaCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ResetUserQuotaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(ResetUserQuotaCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Quota reset attempted for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        var quotaUsedBefore = user.CrawlQuotaUsed;

        // Reset quota usage and set next reset date
        user.CrawlQuotaUsed = 0;
        user.QuotaResetDate = DateTime.UtcNow.AddDays(30); // Reset quota monthly
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quota reset for user {UserId}: Used={UsedBefore} -> 0, NextReset={NextReset}",
            user.Id, quotaUsedBefore, user.QuotaResetDate);

        var data = new
        {
            quotaUsedBefore = quotaUsedBefore,
            nextResetDate = user.QuotaResetDate
        };

        return new ResponseModel(HttpStatusCode.OK, "User quota usage reset successfully", data);
    }
}