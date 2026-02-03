namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Metadata information for chart responses
/// </summary>
public class ChartMetadata
{
    /// <summary>
    /// When the chart data was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time period covered by the data (e.g., "Last 7 days", "2024-01-01 to 2024-01-31")
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Number of data points in the chart
    /// </summary>
    public int? DataPoints { get; set; }

    /// <summary>
    /// Additional metadata specific to the chart type
    /// </summary>
    public Dictionary<string, object>? AdditionalInfo { get; set; }
}
