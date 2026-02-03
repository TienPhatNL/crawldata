namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Query parameters for distribution charts (histograms, bar charts)
/// </summary>
public class DistributionQueryParams : ChartQueryParams
{
    /// <summary>
    /// Number of buckets/bins for distribution
    /// Default: 10, Max: 50
    /// </summary>
    public int BucketCount { get; set; } = 10;

    /// <summary>
    /// Minimum value for distribution range
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Maximum value for distribution range
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// Sort order for bars
    /// Options: "asc", "desc", "value", "label"
    /// Default: "desc"
    /// </summary>
    public string SortBy { get; set; } = "desc";

    /// <summary>
    /// Whether to show outliers
    /// </summary>
    public bool ShowOutliers { get; set; } = true;

    /// <summary>
    /// Percentile for outlier detection (e.g., 95, 99)
    /// </summary>
    public int? OutlierPercentile { get; set; }
}
