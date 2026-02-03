using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Controllers;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Mock implementation of domain validation service for testing
/// TODO: Replace with actual implementation that checks DomainPolicy table
/// </summary>
public class DomainValidationService : IDomainValidationService
{
    private readonly ILogger<DomainValidationService> _logger;

    public DomainValidationService(ILogger<DomainValidationService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsDomainAllowedAsync(string url, SubscriptionTier userTier, UserRole userRole)
    {
        _logger.LogInformation("Domain validation bypassed for testing: {Url}", url);
        return Task.FromResult(true); // Allow all domains for testing
    }

    public Task<UrlValidationResponse> ValidateUrlsAsync(string[] urls, Guid userId, SubscriptionTier userTier)
    {
        _logger.LogInformation("Validating {Count} URLs for user {UserId} (mock validation)", urls.Length, userId);

        var results = urls.Select(url => new UrlValidationResult
        {
            Url = url,
            IsValid = true,
            IsAllowedDomain = true,
            ValidationError = null,
            DomainRestrictionReason = null
        }).ToArray();

        return Task.FromResult(new UrlValidationResponse
        {
            AllUrlsValid = true,
            Results = results,
            InvalidUrls = Array.Empty<string>()
        });
    }

    public Task<bool> CheckRateLimitAsync(string domain, Guid userId)
    {
        _logger.LogInformation("Rate limit check bypassed for testing: {Domain} for user {UserId}", domain, userId);
        return Task.FromResult(true); // No rate limiting for testing
    }

    public Task UpdateDomainPolicyAsync(Guid policyId, bool isActive)
    {
        _logger.LogInformation("Domain policy update bypassed for testing: {PolicyId} -> {IsActive}", policyId, isActive);
        return Task.CompletedTask;
    }
}
