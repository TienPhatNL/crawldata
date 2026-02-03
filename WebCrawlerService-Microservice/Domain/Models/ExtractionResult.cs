using System.Text.Json;

namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Result of LLM-based data extraction from a screen
/// </summary>
public class ExtractionResult
{
    /// <summary>
    /// Extracted data as JSON
    /// </summary>
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");

    /// <summary>
    /// Confidence score from LLM (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Whether extraction was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if extraction failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// LLM model used for extraction
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// Total tokens used (input + output)
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Time taken for extraction
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Additional metadata from extraction
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
