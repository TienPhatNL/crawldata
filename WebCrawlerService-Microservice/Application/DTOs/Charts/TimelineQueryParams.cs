namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Query parameters specific to timeline/time-series charts
/// </summary>
public class TimelineQueryParams : ChartQueryParams
{
    /// <summary>
    /// Time interval for data aggregation
    /// Options: "minute", "hour", "day", "week", "month"
    /// Default: Auto-selected based on date range
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    /// Aggregation function for data points
    /// Options: "sum", "avg", "min", "max", "count"
    /// Default: "sum"
    /// </summary>
    public string AggregationFunction { get; set; } = "sum";

    /// <summary>
    /// Fill missing data points with zero (default: true)
    /// </summary>
    public bool FillGaps { get; set; } = true;

    /// <summary>
    /// Apply smoothing to the line (default: false)
    /// </summary>
    public bool Smooth { get; set; } = false;

    /// <summary>
    /// Show comparison with previous period (default: false)
    /// </summary>
    public bool ShowComparison { get; set; } = false;
}
