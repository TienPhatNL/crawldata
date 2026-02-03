namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Service for managing user crawl quotas
/// </summary>
public interface IUserQuotaService
{
    /// <summary>
    /// Check if user has available quota for crawling
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="requiredUnits">Number of links to consume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has quota available</returns>
    Task<bool> CheckQuotaAsync(Guid userId, int requiredUnits = 1, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deduct crawl links from user's quota
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="units">Number of links to deduct</param>
    /// <param name="jobId">Optional crawl job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeductQuotaAsync(Guid userId, int units, Guid? jobId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get remaining quota for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Remaining quota count</returns>
    Task<int> GetRemainingQuotaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached quota info for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quota info when available</returns>
    Task<WebCrawlerService.Domain.Models.UserQuotaInfo?> GetQuotaInfoAsync(Guid userId, CancellationToken cancellationToken = default);
}
