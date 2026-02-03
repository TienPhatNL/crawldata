namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for time-series charts (line, area)
/// Matches ApexCharts time-series format
/// </summary>
public class TimeSeriesData
{
    /// <summary>
    /// Series data - each series contains name and data points
    /// </summary>
    public List<TimeSeriesItem> Series { get; set; } = new();
}

/// <summary>
/// Single time-series item
/// </summary>
public class TimeSeriesItem
{
    /// <summary>
    /// Series name (e.g., "Successful URLs", "Failed URLs")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data points as [timestamp, value] pairs
    /// Format: [[timestamp_ms, value], [timestamp_ms, value], ...]
    /// </summary>
    public List<object[]> Data { get; set; } = new();
}
