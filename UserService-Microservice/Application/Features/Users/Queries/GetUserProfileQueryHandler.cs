using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Queries;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdWithPlanAsync(request.UserId, cancellationToken);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User profile requested for non-existent user {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        _logger.LogInformation("User profile retrieved for user {UserId}: {FirstName} {LastName}",
            user.Id, user.FirstName, user.LastName);

        // Construct FullName as FirstName + LastName (traditional format)
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        var data = new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            fullName = fullName,
            role = user.Role.ToString(),
            status = user.Status.ToString(),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free",
            isEmailConfirmed = user.IsEmailConfirmed,
            emailConfirmedAt = user.EmailConfirmedAt,
            lastLoginAt = user.LastLoginAt,
            crawlQuotaUsed = user.CrawlQuotaUsed,
            crawlQuotaLimit = user.CrawlQuotaLimit,
            quotaResetDate = user.QuotaResetDate,
            subscriptionStartDate = user.SubscriptionStartDate,
            subscriptionEndDate = user.SubscriptionEndDate,
            institutionName = user.InstitutionName,
            institutionAddress = user.InstitutionAddress,
            studentId = user.StudentId,
            department = user.Department,
            profilePictureUrl = user.ProfilePictureUrl,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt ?? user.CreatedAt
        };

        return new ResponseModel(HttpStatusCode.OK, "User profile retrieved successfully", data);
    }
}