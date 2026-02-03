using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Generic service for mobile API-based crawling
/// </summary>
public interface IMobileApiCrawlerService
{
    /// <summary>
    /// Check if this service can handle the given URL and provider
    /// </summary>
    /// <param name="url">URL to crawl</param>
    /// <param name="provider">Mobile API provider</param>
    /// <returns>True if can handle</returns>
    Task<bool> CanHandleUrl(string url, MobileApiProvider provider);

    /// <summary>
    /// Get configuration for a specific mobile API provider
    /// </summary>
    /// <param name="provider">Mobile API provider</param>
    /// <returns>API configuration</returns>
    Task<MobileApiConfiguration> GetConfigurationAsync(MobileApiProvider provider);

    /// <summary>
    /// Execute a generic API call with the given configuration
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint path</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="config">Mobile API configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T> ExecuteApiCallAsync<T>(string endpoint, Dictionary<string, string> parameters,
        MobileApiConfiguration config, CancellationToken cancellationToken = default);
}
