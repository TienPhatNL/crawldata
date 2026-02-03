using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Queries;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(IUnitOfWork unitOfWork, ILogger<GetUserQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdWithPlanAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", request.UserId);
            throw new ValidationException("User not found");
        }

        // Authorization check - users can only view their own profile unless they are admin/staff
        if (request.RequestingUserId.HasValue && 
            request.RequestingUserId != request.UserId)
        {
            var requestingUser = await _unitOfWork.Users.GetByIdAsync(request.RequestingUserId.Value, cancellationToken);
            if (requestingUser?.Role != UserRole.Admin && requestingUser?.Role != UserRole.Staff)
            {
                throw new ValidationException("Unauthorized to view this user profile");
            }
        }

        var data = new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            fullName = user.FullName,
            phoneNumber = user.PhoneNumber,
            role = user.Role.ToString(),
            roleDisplay = GetRoleDisplay(user.Role),
            status = user.Status.ToString(),
            statusDisplay = GetStatusDisplay(user.Status),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free",
            subscriptionTierDisplay = GetSubscriptionTierDisplay(user.SubscriptionTier?.Level ?? 0),

            // Quota information
            crawlQuotaUsed = user.CrawlQuotaUsed,
            crawlQuotaLimit = user.CrawlQuotaLimit,
            quotaResetDate = user.QuotaResetDate,
            quotaRemaining = Math.Max(0, user.CrawlQuotaLimit - user.CrawlQuotaUsed),

            // Account information
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt,
            isEmailConfirmed = user.IsEmailConfirmed,
            requiresApproval = user.RequiresApproval,
            isApproved = user.IsApproved,
            approvedAt = user.ApprovedAt,

            // Profile information
            profilePictureUrl = user.ProfilePictureUrl,
            biography = user.Biography,
            timeZone = user.TimeZone,
            preferredLanguage = user.PreferredLanguage,

            // Role-specific information
            institutionName = user.InstitutionName,
            institutionEmail = user.InstitutionEmail,
            department = user.Department,
            position = user.Position,
            studentId = user.StudentId,
            staffDepartment = user.StaffDepartment,
            adminLevel = user.AdminLevel,

            // Security settings
            twoFactorEnabled = user.TwoFactorEnabled,
            passwordChangedAt = user.PasswordChangedAt,

            // Subscription details
            currentSubscription = user.CurrentSubscription != null ? new
            {
                id = user.CurrentSubscription.Id,
                tier = user.CurrentSubscription.SubscriptionPlan?.Tier?.Name ?? "Free",
                tierDisplay = GetSubscriptionTierDisplay(user.CurrentSubscription.SubscriptionPlan?.Tier?.Level ?? 0),
                planName = user.CurrentSubscription.SubscriptionPlan?.Name,
                startDate = user.CurrentSubscription.StartDate,
                endDate = user.CurrentSubscription.EndDate,
                isActive = user.CurrentSubscription.IsActive,
                autoRenew = user.CurrentSubscription.AutoRenew,
                nextBillingDate = user.CurrentSubscription.NextBillingDate,
                price = user.CurrentSubscription.Price,
                currency = user.CurrentSubscription.Currency,
                crawlUrlLimit = user.CurrentSubscription.QuotaLimit,
                dataExtractionLimit = user.CurrentSubscription.DataExtractionLimit,
                reportGenerationLimit = user.CurrentSubscription.ReportGenerationLimit,
                advancedAnalyticsEnabled = user.CurrentSubscription.AdvancedAnalyticsEnabled,
                prioritySupport = user.CurrentSubscription.PrioritySupport,
                apiAccessEnabled = user.CurrentSubscription.ApiAccessEnabled
            } : null
        };

        return new ResponseModel(HttpStatusCode.OK, "User retrieved successfully", data);
    }

    private static string GetRoleDisplay(UserRole role)
    {
        return role switch
        {
            UserRole.Student => "Student",
            UserRole.Lecturer => "Lecturer",
            UserRole.Staff => "Staff",
            UserRole.Admin => "Administrator",
            UserRole.PaidUser => "Paid User",
            _ => role.ToString()
        };
    }

    private static string GetStatusDisplay(UserStatus status)
    {
        return status switch
        {
            UserStatus.Pending => "Pending Email Confirmation",
            UserStatus.Active => "Active",
            UserStatus.Inactive => "Inactive",
            UserStatus.Suspended => "Suspended",
            UserStatus.Deleted => "Deleted",
            UserStatus.PendingApproval => "Pending Staff Approval",
            _ => status.ToString()
        };
    }

    private static string GetSubscriptionTierDisplay(int tierLevel)
    {
        return tierLevel switch
        {
            0 => "Free (Students)",
            1 => "Basic (Lecturers)",
            2 => "Premium (Staff)",
            3 => "Enterprise (Admin)",
            _ => $"Level {tierLevel}"
        };
    }
}