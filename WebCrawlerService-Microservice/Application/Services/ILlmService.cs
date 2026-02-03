using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for interacting with Large Language Models (OpenAI, Claude, etc.)
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Generate an extraction strategy from a natural language prompt
    /// </summary>
    Task<ExtractionStrategy> GenerateExtractionStrategyAsync(
        string userPrompt,
        string sampleHtml,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract structured data using AI from HTML content
    /// </summary>
    Task<Dictionary<string, object>> ExtractDataWithAiAsync(
        string html,
        ExtractionStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate and improve existing extraction strategy
    /// </summary>
    Task<ExtractionStrategy> ValidateAndImproveStrategyAsync(
        ExtractionStrategy strategy,
        string sampleHtml,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimate the cost of extraction for a given strategy
    /// </summary>
    Task<decimal> EstimateCostAsync(
        ExtractionStrategy strategy,
        int estimatedPageCount,
        CancellationToken cancellationToken = default);
}
