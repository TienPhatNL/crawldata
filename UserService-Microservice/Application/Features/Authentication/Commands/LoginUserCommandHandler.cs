using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.Commands;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IWebCrawlerQuotaCacheWriter _webCrawlerQuotaCacheWriter;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        IWebCrawlerQuotaCacheWriter webCrawlerQuotaCacheWriter,
        ILogger<LoginUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
        _webCrawlerQuotaCacheWriter = webCrawlerQuotaCacheWriter;
        _logger = logger;
    }

    public async Task<ResponseModel> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email} from IP: {IpAddress}", request.Email, request.IpAddress);

        // Get user by email
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", request.Email);
            throw new ValidationException("Invalid email or password");
        }

        // Log user state for debugging with timestamp
        _logger.LogDebug("User found for login at {Timestamp}: UserId={UserId}, Status={Status}, IsEmailConfirmed={IsEmailConfirmed}, EmailConfirmedAt={EmailConfirmedAt}, CanLogin={CanLogin}",
            DateTime.UtcNow, user.Id, user.Status, user.IsEmailConfirmed, user.EmailConfirmedAt, user.CanLogin);

        // Check if account is locked
        if (user.IsLocked)
        {
            _logger.LogWarning("Login attempt failed: Account locked for user {UserId}", user.Id);
            throw new ValidationException($"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss}");
        }

        // Check if this is a Google-authenticated user (no password set)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Login attempt with password for Google-authenticated user {UserId}", user.Id);
            throw new ValidationException("This account uses Google authentication. Please sign in with Google.");
        }

        // Verify password
        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            
            // Publish security alert if multiple failed attempts
            if (user.FailedLoginAttempts >= 3)
            {
                var alertType = user.FailedLoginAttempts >= 5 ? "AccountLocked" : "FailedAttempts";
                var severity = user.FailedLoginAttempts >= 5 ? "Critical" : "High";
                var details = user.FailedLoginAttempts >= 5 
                    ? $"Account locked after {user.FailedLoginAttempts} failed login attempts"
                    : $"{user.FailedLoginAttempts} failed login attempts detected";

                // Add domain event for security alert (will be auto-dispatched by SaveChangesAsync)
                user.AddDomainEvent(new SecurityAlertEvent(
                    user.Id,
                    alertType,
                    details,
                    DateTime.UtcNow,
                    severity,
                    request.IpAddress,
                    request.UserAgent,
                    null)); // Location not available
            }
            
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30); // Lock for 30 minutes
                _logger.LogWarning("Account locked due to too many failed attempts for user {UserId}", user.Id);
            }

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning("Login attempt failed: Invalid password for user {UserId}", user.Id);
            throw new ValidationException("Invalid email or password");
        }

        // Refresh user data from database to ensure we have the latest state
        // This prevents issues where email confirmation happens just before login
        var userId = user.Id;
        user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found during refresh check", userId);
            throw new ValidationException("Invalid email or password");
        }

        // Log refreshed user state for debugging
        _logger.LogDebug("User state after refresh at {Timestamp}: UserId={UserId}, Status={Status}, IsEmailConfirmed={IsEmailConfirmed}, EmailConfirmedAt={EmailConfirmedAt}, CanLogin={CanLogin}",
            DateTime.UtcNow, user.Id, user.Status, user.IsEmailConfirmed, user.EmailConfirmedAt, user.CanLogin);

        // Check account status with detailed logging
        if (!user.CanLogin)
        {
            // Log detailed state for debugging with timestamp
            _logger.LogWarning("Login attempt failed for user {UserId} at {Timestamp}: Status={Status}, IsEmailConfirmed={IsEmailConfirmed}, EmailConfirmedAt={EmailConfirmedAt}, IsLocked={IsLocked}",
                user.Id, DateTime.UtcNow, user.Status, user.IsEmailConfirmed, user.EmailConfirmedAt, user.IsLocked);

            var reason = user.Status switch
            {
                UserStatus.Pending when !user.IsEmailConfirmed => "Account is pending email confirmation. Please check your email and click the confirmation link.",
                UserStatus.Pending when user.IsEmailConfirmed => "Account activation is in progress. Please try again in a few moments.",
                UserStatus.PendingApproval => "Account is pending staff approval. You will be notified via email once approved.",
                UserStatus.Inactive => "Account is inactive. Please contact support for assistance.",
                UserStatus.Suspended => "Account is suspended. Please contact support for more information.",
                UserStatus.Deleted => "Account is no longer available.",
                _ when user.IsLocked => $"Account is temporarily locked due to multiple failed login attempts. Please try again after {user.LockedUntil:yyyy-MM-dd HH:mm:ss} UTC.",
                _ when !user.IsEmailConfirmed => "Email address has not been confirmed. Please check your email and click the confirmation link.",
                _ => "Account cannot login at this time. Please contact support for assistance."
            };

            _logger.LogWarning("Login attempt failed: {Reason} for user {UserId}", reason, user.Id);
            throw new ValidationException(reason);
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        // Generate tokens
        var claims = _jwtTokenService.GetUserClaims(user.Id, user.Email, user.Role.ToString(), user.SubscriptionTier?.Name ?? "Free");
        var accessToken = _jwtTokenService.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenExpires = DateTime.UtcNow.AddMinutes(60); // Default token expiry

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

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        var data = new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            role = user.Role.ToString(),
            subscriptionTier = user.SubscriptionTier?.Name ?? "Free",
            accessToken = accessToken,
            refreshToken = refreshToken,
            tokenExpires = tokenExpires,
            crawlQuotaUsed = user.CrawlQuotaUsed,
            crawlQuotaLimit = user.CrawlQuotaLimit,
            quotaResetDate = user.QuotaResetDate,
            requiresEmailConfirmation = !user.IsEmailConfirmed,
            requiresApproval = user.Status == UserStatus.PendingApproval
        };

        return new ResponseModel(HttpStatusCode.OK, "Login successful", data);
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
