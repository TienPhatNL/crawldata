using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching users with filters: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, Role={Role}, Status={Status}",
            request.Page, request.PageSize, request.SearchTerm, request.Role, request.Status);

        // If emails are specified, use batch lookup (PNLT feature for ClassroomService)
        if (request.Emails != null && request.Emails.Any())
        {
            var usersByEmail = await _unitOfWork.Users.GetUsersByEmailsAsync(request.Emails, cancellationToken);

            var emailUserDtos = usersByEmail.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                firstName = u.FirstName,
                lastName = u.LastName,
                role = u.Role.ToString(),
                status = u.Status.ToString(),
                subscriptionTier = u.SubscriptionTier != null ? u.SubscriptionTier.Name : "Free",
                isEmailConfirmed = u.IsEmailConfirmed,
                lastLoginAt = u.LastLoginAt,
                crawlQuotaUsed = u.CrawlQuotaUsed,
                crawlQuotaLimit = u.CrawlQuotaLimit,
                institutionName = u.InstitutionName,
                studentId = u.StudentId,
                profilePictureUrl = u.ProfilePictureUrl,
                createdAt = u.CreatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {UserCount} users by email batch lookup", emailUserDtos.Count);

            var emailData = new
            {
                users = emailUserDtos,
                totalCount = emailUserDtos.Count,
                page = 1,
                pageSize = emailUserDtos.Count,
                totalPages = 1
            };

            return new ResponseModel(HttpStatusCode.OK, "Users retrieved successfully", emailData);
        }

        var (users, totalCount) = await _unitOfWork.Users.GetUsersPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.Role,
            request.Status,
            request.SubscriptionTierName,
            request.SortBy,
            request.SortOrder,
            cancellationToken);

        var userDtos = users.Select(u => new
        {
            id = u.Id,
            email = u.Email,
            firstName = u.FirstName,
            lastName = u.LastName,
            role = u.Role.ToString(),
            status = u.Status.ToString(),
            subscriptionTier = u.SubscriptionTier != null ? u.SubscriptionTier.Name : "Free",
            isEmailConfirmed = u.IsEmailConfirmed,
            lastLoginAt = u.LastLoginAt,
            crawlQuotaUsed = u.CrawlQuotaUsed,
            crawlQuotaLimit = u.CrawlQuotaLimit,
            institutionName = u.InstitutionName,
            studentId = u.StudentId,
            profilePictureUrl = u.ProfilePictureUrl,
            createdAt = u.CreatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        _logger.LogInformation("Retrieved {UserCount} users out of {TotalCount} total", userDtos.Count, totalCount);

        var data = new
        {
            users = userDtos,
            totalCount = totalCount,
            page = request.Page,
            pageSize = request.PageSize,
            totalPages = totalPages
        };

        return new ResponseModel(HttpStatusCode.OK, "Users retrieved successfully", data);
    }
}