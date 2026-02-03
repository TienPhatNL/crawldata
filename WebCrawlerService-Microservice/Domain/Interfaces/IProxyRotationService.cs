using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Service for managing proxy rotation and health checks
/// </summary>
public interface IProxyRotationService
{
    /// <summary>
    /// Get next available proxy from the pool
    /// </summary>
    Task<ProxyConfiguration?> GetNextProxyAsync(
        string? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark proxy as failed
    /// </summary>
    Task MarkProxyAsFailedAsync(
        string proxyUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark proxy as successful
    /// </summary>
    Task MarkProxyAsSuccessfulAsync(
        string proxyUrl,
        int responseTimeMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get proxy pool statistics
    /// </summary>
    Task<ProxyPoolStats> GetProxyPoolStatsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform health check on all proxies
    /// </summary>
    Task PerformHealthCheckAsync(CancellationToken cancellationToken = default);
}

public class ProxyPoolStats
{
    public int TotalProxies { get; set; }
    public int ActiveProxies { get; set; }
    public int FailedProxies { get; set; }
    public double AverageResponseTime { get; set; }
}
