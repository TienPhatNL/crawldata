using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Domain.Interfaces;

/// <summary>
/// Service for extracting structured data from mobile app screens using LLMs
/// </summary>
public interface ILlmExtractionService
{
    /// <summary>
    /// Extract data from a screen state using the specified schema
    /// </summary>
    /// <param name="screenState">Current screen state (screenshot + UI hierarchy)</param>
    /// <param name="schema">Schema defining what data to extract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extraction result with structured data</returns>
    Task<ExtractionResult> ExtractAsync(
        ScreenState screenState,
        ExtractionSchema schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract data using a custom prompt instead of a schema
    /// </summary>
    /// <param name="screenState">Current screen state</param>
    /// <param name="prompt">Custom extraction prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extraction result</returns>
    Task<ExtractionResult> ExtractWithPromptAsync(
        ScreenState screenState,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if current screen matches expected screen type
    /// Useful for navigation verification
    /// </summary>
    /// <param name="screenState">Current screen state</param>
    /// <param name="expectedScreenType">Expected screen type (e.g., "product_detail", "search_results")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if screen matches expected type</returns>
    Task<bool> ValidateScreenTypeAsync(
        ScreenState screenState,
        string expectedScreenType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine next action to take based on current screen and goal
    /// </summary>
    /// <param name="screenState">Current screen state</param>
    /// <param name="goal">What we're trying to accomplish (e.g., "navigate to product reviews")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested next action (e.g., "tap element with text 'Reviews'")</returns>
    Task<string> DetermineNextActionAsync(
        ScreenState screenState,
        string goal,
        CancellationToken cancellationToken = default);
}
