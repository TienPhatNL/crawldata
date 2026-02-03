namespace WebCrawlerService.Infrastructure.Common;

/// <summary>
/// Configuration settings for Google Gemini LLM provider
/// </summary>
public class LlmSettings
{
    public const string SectionName = "LlmSettings";

    /// <summary>
    /// Google Gemini API key
    /// </summary>
    public string? GeminiApiKey { get; init; }

    /// <summary>
    /// Gemini model to use (e.g., "gemini-1.5-pro", "gemini-1.5-flash")
    /// </summary>
    public string GeminiModel { get; init; } = "gemini-1.5-pro";

    /// <summary>
    /// Maximum tokens for LLM response
    /// </summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Enable caching of extraction results for identical screens
    /// </summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Cache expiration in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; init; } = 60;
}
