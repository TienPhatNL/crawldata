namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for mixed/multi-series charts
/// Supports combining different chart types (line + bar, area + line, etc.)
/// </summary>
public class MultiSeriesData
{
    /// <summary>
    /// Series data with type specification for each series
    /// </summary>
    public List<MultiSeriesItem> Series { get; set; } = new();

    /// <summary>
    /// Categories for X-axis
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Single multi-series item with type specification
/// </summary>
public class MultiSeriesItem
{
    /// <summary>
    /// Series name (e.g., "CPU Usage", "Memory Usage")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Chart type for this series (e.g., "line", "column", "area")
    /// </summary>
    public string Type { get; set; } = "line";

    /// <summary>
    /// Data values
    /// </summary>
    public double[] Data { get; set; } = Array.Empty<double>();
}
