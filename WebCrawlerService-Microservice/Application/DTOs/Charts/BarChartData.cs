namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for bar and column charts
/// Matches ApexCharts bar chart format
/// </summary>
public class BarChartData
{
    /// <summary>
    /// Series data - can have multiple series for grouped bars
    /// </summary>
    public List<BarSeriesItem> Series { get; set; } = new();

    /// <summary>
    /// Categories for X-axis (bar labels)
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Single bar series item
/// </summary>
public class BarSeriesItem
{
    /// <summary>
    /// Series name (e.g., "Response Time", "Success Count")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data values for each category
    /// </summary>
    public double[] Data { get; set; } = Array.Empty<double>();
}
