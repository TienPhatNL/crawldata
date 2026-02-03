namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for stacked bar/area charts
/// Matches ApexCharts stacked format
/// </summary>
public class StackedBarData
{
    /// <summary>
    /// Series data - each series stacks on top of previous
    /// </summary>
    public List<StackedSeriesItem> Series { get; set; } = new();

    /// <summary>
    /// Categories for X-axis
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional total values for each category
    /// </summary>
    public double[]? Totals { get; set; }
}

/// <summary>
/// Single stacked series item
/// </summary>
public class StackedSeriesItem
{
    /// <summary>
    /// Series name (e.g., "Active Jobs", "Completed Jobs")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data values for each category
    /// </summary>
    public double[] Data { get; set; } = Array.Empty<double>();
}
