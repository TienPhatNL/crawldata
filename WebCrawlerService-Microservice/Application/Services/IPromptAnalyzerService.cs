using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for analyzing user prompts and determining the best crawl strategy
/// </summary>
public interface IPromptAnalyzerService
{
    /// <summary>
    /// Analyze user prompt and recommend the best crawler type
    /// </summary>
    Task<CrawlerType> RecommendCrawlerTypeAsync(
        string userPrompt,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find matching template for given URL and user intent
    /// </summary>
    Task<CrawlTemplate?> FindMatchingTemplateAsync(
        string url,
        string? userPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate extraction strategy from user prompt
    /// </summary>
    Task<ExtractionStrategy> GenerateStrategyFromPromptAsync(
        string userPrompt,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if user prompt is safe and appropriate
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidatePromptAsync(
        string userPrompt,
        CancellationToken cancellationToken = default);
}
