namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Represents the complete state of a mobile app screen
/// Used for LLM-based data extraction
/// </summary>
public class ScreenState
{
    /// <summary>
    /// Base64-encoded PNG screenshot
    /// </summary>
    public string Screenshot { get; set; } = string.Empty;

    /// <summary>
    /// Cleaned XML UI hierarchy from UiAutomator
    /// </summary>
    public string UiHierarchy { get; set; } = string.Empty;

    /// <summary>
    /// Array of visible text elements extracted from UI hierarchy
    /// </summary>
    public List<string> VisibleText { get; set; } = new();

    /// <summary>
    /// Timestamp when screen state was captured
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional metadata about the screen
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
