namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Basic chart configuration matching ApexCharts chart options
/// </summary>
public class ChartConfiguration
{
    /// <summary>
    /// Chart type (e.g., "line", "area", "bar", "pie", "donut", "radialBar", etc.)
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Chart title
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Optional subtitle
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Whether to stack series (for bar/area charts)
    /// </summary>
    public bool? Stacked { get; set; }

    /// <summary>
    /// Whether to display chart horizontally (for bar charts)
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Height of the chart (e.g., "350px", "auto")
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Width of the chart (e.g., "100%", "500px")
    /// </summary>
    public string? Width { get; set; }
}
