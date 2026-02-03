using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Features.Authentication.Commands;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IWebCrawlerQuotaCacheWriter _webCrawlerQuotaCacheWriter;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IUnitOfWork unitOfWork,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService,
        IWebCrawlerQuotaCacheWriter webCrawlerQuotaCacheWriter,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
        _webCrawlerQuotaCacheWriter = webCrawlerQuotaCacheWriter;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Google login attempt from IP: {IpAddress}", request.IpAddress);

        // Validate Google token
        Google.Apis.Auth.GoogleJsonWebSignature.Payload googleUser;
        try
        {
            googleUser = await _googleAuthService.ValidateGoogleTokenAsync(request.GoogleIdToken, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
            throw new ValidationException("Invalid Google token. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            throw new ValidationException("Google authentication service error. Please try again later.");
        }

        if (string.IsNullOrWhiteSpace(googleUser.Email))
        {
            _logger.LogWarning("Google token validation succeeded but email is missing");
            throw new ValidationException("Unable to retrieve email from Google account");
        }

        _logger.LogInformation("Google token validated for email: {Email}", googleUser.Email);

        // Check if user exists by email
        var user = await _unitOfWork.Users.GetByEmailAsync(googleUser.Email.ToLowerInvariant(), cancellationToken);
        var isNewUser = false;

        if (user == null)
        {
            // Auto-register new user from Google
            _logger.LogInformation("Creating new user from Google account: {Email}", googleUser.Email);
            isNewUser = true;

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = googleUser.Email.ToLowerInvariant(),
                PasswordHash = "", // Empty for Google users (no password)
                FirstName = googleUser.GivenName ?? "User",
                LastName = googleUser.FamilyName ?? "",
                Role = UserRole.Student, // Default role for Google users
                Status = UserStatus.Active, // Auto-activate Google users
                EmailConfirmedAt = DateTime.UtcNow, // Google emails are pre-verified
                ProfilePictureUrl = googleUser.Picture,
                CurrentSubscriptionPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Free plan
                CrawlQuotaLimit = 4,
                CrawlQuotaUsed = 0,
                QuotaResetDate = GetNextQuotaResetDate(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            _logger.LogInformation("New user created via Google: UserId={UserId}, Email={Email}", user.Id, user.Email);
        }
        else
        {
            // Existing user logging in via Google
            _logger.LogInformation("Existing user logging in via Google: UserId={UserId}, Email={Email}", user.Id, user.Email);

            // Update profile picture if available
            if (!string.IsNullOrEmpty(googleUser.Picture))
            {
                user.ProfilePictureUrl = googleUser.Picture;
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                _logger.LogWarning("Google login attempt failed: Account locked for user {UserId}", user.Id);
                throw new ValidationException($"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss}");
            }

            // Check account status
            if (!user.CanLogin)
            {
                var reason = user.Status switch
                {
                    UserStatus.Pending => "Account is pending activation. Please contact support.",
                    UserStatus.PendingApproval => "Account is pending staff approval. You will be notified via email once approved.",
                    UserStatus.Inactive => "Account is inactive. Please contact support for assistance.",
                    UserStatus.Suspended => "Account is suspended. Please contact support for more information.",
                    UserStatus.Deleted => "Account is no longer available.",
                    _ => "Account cannot login at this time. Please contact support for assistance."
                };

                _logger.LogWarning("Google login attempt failed: {Reason} for user {UserId}", reason, user.Id);
                throw new ValidationException(reason);
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        }

        // Generate tokens
        var claims = _jwtTokenService.GetUserClaims(user.Id, user.Email, user.Role.ToString(), user.SubscriptionTier?.Name ?? "Free");
        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenExpires = DateTime.UtcNow.AddMinutes(60);

        // Create session
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = tokenExpires,
            RefreshTokenExpiresAt = request.RememberMe
                ? DateTime.UtcNow.AddDays(30)
                : DateTime.UtcNow.AddDays(7),
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SeedWebCrawlerQuotaCacheAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully via Google", user.Id);

        var data = new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            profilePictureUrl = user.ProfilePictureUrl,
            role = user.Role.ToString(),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free",
            accessToken = accessToken,
            refreshToken = refreshToken,
            tokenExpires = tokenExpires,
            crawlQuotaUsed = user.CrawlQuotaUsed,
            crawlQuotaLimit = user.CrawlQuotaLimit,
            quotaResetDate = user.QuotaResetDate,
            isNewUser = isNewUser
        };

        return new ResponseModel(HttpStatusCode.OK, "Google login successful", data);
    }

    private static DateTime GetNextQuotaResetDate()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
    }

    private async Task SeedWebCrawlerQuotaCacheAsync(User user, CancellationToken cancellationToken)
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
                user.SubscriptionTier?.Name ?? "Free",
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
}
