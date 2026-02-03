using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserService.Application.Common.Interfaces;
using UserService.Application.Common.Models;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Events;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using UserService.Infrastructure.Messaging;

namespace UserService.Application.Features.Authentication.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ResponseModel>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public RegisterUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        IMediator mediator,
        ILogger<RegisterUserCommandHandler> logger,
        IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _mediator = mediator;
        _logger = logger;
        _cache = cache;
    }

    public async Task<ResponseModel> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ValidationException("Email already exists");
        }

        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // Determine if approval is required based on role
            var requiresApproval = request.Role == UserRole.Lecturer;
            var planId = GetDefaultPlanIdForRole(request.Role);
            
            // Fetch the plan to get quota limit (only for roles that get automatic plans)
            int quotaLimit = 4; // Default quota
            if (planId.HasValue)
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(planId.Value, cancellationToken);
                if (plan == null)
                {
                    throw new ValidationException($"Default subscription plan not found for role {request.Role}");
                }
                quotaLimit = plan.QuotaLimit;
            }

            // Create user entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = _passwordHashingService.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                Status = requiresApproval ? UserStatus.PendingApproval : UserStatus.Pending,
                CurrentSubscriptionPlanId = planId,
                CrawlQuotaLimit = quotaLimit,
                QuotaResetDate = GetNextQuotaResetDate(),
                RequiresApproval = requiresApproval,

                // Role-specific fields
                InstitutionName = request.InstitutionName,
                InstitutionEmail = request.InstitutionEmail,
                Department = request.Department,
                Position = request.Position,
                StudentId = request.StudentId,

                // Email verification
                EmailVerificationToken = GenerateVerificationToken(),
                EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24),

                CreatedAt = DateTime.UtcNow
            };

            // Add domain event (will be auto-dispatched by ExecuteTransactionAsync)
            user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Role, requiresApproval));

            // Save user
            await _unitOfWork.Users.AddAsync(user, cancellationToken);

            _logger.LogInformation("User {UserId} registered with email {Email} and role {Role}",
                user.Id, user.Email, user.Role);

            // Invalidate dashboard caches to show new user immediately
            await InvalidateDashboardCachesAsync(cancellationToken);

            var message = requiresApproval
                ? "Registration successful. Your account is pending approval by staff."
                : "Registration successful. Please check your email to confirm your account.";

            var data = new
            {
                userId = user.Id,
                email = user.Email,
                requiresEmailConfirmation = true,
                requiresApproval = requiresApproval
            };

            return new ResponseModel(HttpStatusCode.OK, message, data);
        }, cancellationToken);
    }

    private static Guid? GetDefaultPlanIdForRole(UserRole role)
    {
        // Only students get automatic Free plan assignment
        // Lecturers and Staff don't need crawling features by default
        return role switch
        {
            UserRole.Student => Guid.Parse("11111111-1111-1111-1111-111111111111"), // Free plan
            _ => null // No automatic plan for other roles
        };
    }

    private static DateTime GetNextQuotaResetDate()
    {
        return new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "").Replace("/", "");
    }

    private async Task InvalidateDashboardCachesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate all dashboard-related caches by removing cache keys with specific prefixes
            // Note: Redis doesn't support pattern-based deletion in IDistributedCache interface,
            // so we clear common cache keys that would be affected by new user registration

            var cacheKeysToRemove = new List<string>();

            // Generate cache keys for common date ranges (last 30, 60, 90 days)
            var today = DateTime.UtcNow.Date;
            var dateRanges = new[]
            {
                (today.AddDays(-7), today),    // Last 7 days
                (today.AddDays(-30), today),   // Last 30 days
                (today.AddDays(-60), today),   // Last 60 days
                (today.AddDays(-90), today),   // Last 90 days
                (today.AddMonths(-6), today),  // Last 6 months
                (today.AddYears(-1), today)    // Last year
            };

            // Common interval values used in dashboard queries
            var intervals = new[] { "day", "week", "month" };

            // User statistics cache keys (with various quota thresholds)
            var quotaThresholds = new[] { 0, 5, 10, 20, 50, 75, 90 };
            
            foreach (var (startDate, endDate) in dateRanges)
            {
                foreach (var threshold in quotaThresholds)
                {
                    cacheKeysToRemove.Add($"admin:dashboard:users:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{threshold}");
                }
            }

            // Overview cache keys (with different interval values)
            foreach (var (startDate, endDate) in dateRanges)
            {
                foreach (var interval in intervals)
                {
                    cacheKeysToRemove.Add($"admin:dashboard:overview:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{interval}");
                }
            }

            // Subscription statistics cache keys
            foreach (var (startDate, endDate) in dateRanges)
            {
                cacheKeysToRemove.Add($"admin:dashboard:subscriptions:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}");
            }

            // Remove all cache keys
            _logger.LogInformation("Attempting to invalidate {Count} dashboard cache keys after user registration", cacheKeysToRemove.Count);
            
            var removedCount = 0;
            foreach (var cacheKey in cacheKeysToRemove)
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                removedCount++;
            }

            _logger.LogInformation("Successfully invalidated {Count} dashboard cache keys after user registration", removedCount);
        }
        catch (Exception ex)
        {
            // Don't fail the registration if cache invalidation fails
            _logger.LogWarning(ex, "Failed to invalidate dashboard caches after user registration");
        }
    }
}