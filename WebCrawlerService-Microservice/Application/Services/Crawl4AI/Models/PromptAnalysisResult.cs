namespace WebCrawlerService.Application.Services.Crawl4AI.Models;

/// <summary>
/// Result of analyzing a user's natural language prompt
/// </summary>
public class PromptAnalysisResult
{
    /// <summary>
    /// What the user wants to do
    /// </summary>
    public string Intent { get; set; } = null!;

    /// <summary>
    /// Extracted entities (brand names, categories, etc.)
    /// </summary>
    public Dictionary<string, object> Entities { get; set; } = new();

    /// <summary>
    /// Target category if mentioned
    /// </summary>
    public string? TargetCategory { get; set; }

    /// <summary>
    /// Target brand if mentioned
    /// </summary>
    public string? TargetBrand { get; set; }

    /// <summary>
    /// Type of data to extract
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Whether navigation is needed (filtering, category selection, etc.)
    /// </summary>
    public bool RequiresNavigation { get; set; }

    /// <summary>
    /// Confidence score (0-1)
    /// </summary>
    public double Confidence { get; set; } = 1.0;
}
