namespace WebCrawlerService.Application.DTOs.DataVisualization;

/// <summary>
/// Options for customizing chart visualization
/// </summary>
public class VisualizationOptions
{
    /// <summary>
    /// Custom title for the chart (if not specified, AI will generate)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Field to use for X-axis or categories
    /// </summary>
    public string? XAxisField { get; set; }

    /// <summary>
    /// Field to use for Y-axis or values
    /// </summary>
    public string? YAxisField { get; set; }

    /// <summary>
    /// Aggregation function to apply (sum, avg, count, min, max)
    /// </summary>
    public string? AggregationFunction { get; set; }

    /// <summary>
    /// Filters to apply to the data before visualization
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }

    /// <summary>
    /// Maximum number of data points/categories to show
    /// </summary>
    public int? MaxDataPoints { get; set; }

    /// <summary>
    /// Sort order (asc, desc, or field name)
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// Color scheme for the chart (default, vibrant, pastel, monochrome)
    /// </summary>
    public string? ColorScheme { get; set; }

    /// <summary>
    /// Whether to show data labels on the chart
    /// </summary>
    public bool ShowDataLabels { get; set; } = true;

    /// <summary>
    /// Whether to show legend
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Group by field (for multi-series charts)
    /// </summary>
    public string? GroupByField { get; set; }

    /// <summary>
    /// Custom color palette (hex codes)
    /// </summary>
    public List<string>? CustomColors { get; set; }

    /// <summary>
    /// Chart height in pixels
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Chart width in pixels
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Whether to enable animations
    /// </summary>
    public bool Animated { get; set; } = true;

    /// <summary>
    /// Whether to include only top N values (e.g., top 10 products by price)
    /// </summary>
    public int? TopN { get; set; }

    /// <summary>
    /// Custom formatting for values (e.g., currency, percentage)
    /// </summary>
    public string? ValueFormat { get; set; }

    /// <summary>
    /// Additional custom options as key-value pairs
    /// </summary>
    public Dictionary<string, object>? CustomOptions { get; set; }
}
