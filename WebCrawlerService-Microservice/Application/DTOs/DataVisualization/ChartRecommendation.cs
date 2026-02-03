namespace WebCrawlerService.Application.DTOs.DataVisualization;

/// <summary>
/// AI-generated recommendation for a specific chart type
/// </summary>
public class ChartRecommendation
{
    /// <summary>
    /// Type of chart (pie, bar, line, scatter, histogram, radial, stacked-bar)
    /// </summary>
    public string ChartType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title for the chart
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// AI explanation of why this chart type is recommended
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Data field(s) to use for X-axis or categories
    /// </summary>
    public List<string> XAxisFields { get; set; } = new();

    /// <summary>
    /// Data field(s) to use for Y-axis or values
    /// </summary>
    public List<string> YAxisFields { get; set; } = new();

    /// <summary>
    /// Optional pre-generated chart data (e.g. from Python agent)
    /// </summary>
    public object? PreGeneratedData { get; set; }

    /// <summary>
    /// Optional grouping/aggregation function (sum, avg, count, etc.)
    /// </summary>
    public string? AggregationFunction { get; set; }

    /// <summary>
    /// Confidence score for this recommendation (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Priority ranking (1 = highest priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Insights that this chart will reveal
    /// </summary>
    public List<string> ExpectedInsights { get; set; } = new();

    /// <summary>
    /// Optional filters to apply to the data
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }

    /// <summary>
    /// Suggested color scheme for the chart
    /// </summary>
    public string? ColorScheme { get; set; }
}
