using WebCrawlerService.Application.Controllers;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Application.Services
{
    public interface IDomainValidationService
    {
        Task<UrlValidationResponse> ValidateUrlsAsync(string[] urls, Guid userId, SubscriptionTier userTier);
        Task<bool> IsDomainAllowedAsync(string url, SubscriptionTier userTier, UserRole userRole);
        Task<bool> CheckRateLimitAsync(string domain, Guid userId);
        Task UpdateDomainPolicyAsync(Guid policyId, bool isActive);
    }
}