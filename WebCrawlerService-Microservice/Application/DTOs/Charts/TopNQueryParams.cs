namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Query parameters for "Top N" charts (e.g., top domains, top errors)
/// </summary>
public class TopNQueryParams : ChartQueryParams
{
    /// <summary>
    /// Number of top items to return
    /// Default: 10, Max: 50
    /// </summary>
    public int TopN { get; set; } = 10;

    /// <summary>
    /// Sort order
    /// Options: "desc" (highest first), "asc" (lowest first)
    /// Default: "desc"
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// Metric to sort by
    /// Options: "count", "total_size", "avg_response_time", "success_rate"
    /// Default: "count"
    /// </summary>
    public string SortBy { get; set; } = "count";

    /// <summary>
    /// Whether to group remaining items as "Others"
    /// </summary>
    public bool GroupOthers { get; set; } = true;

    /// <summary>
    /// Minimum threshold to be included (e.g., minimum 5 requests)
    /// </summary>
    public int? MinThreshold { get; set; }
}
