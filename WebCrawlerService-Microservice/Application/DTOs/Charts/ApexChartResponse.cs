namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Generic response wrapper for ApexCharts-compatible data
/// This structure matches ApexCharts options format for minimal frontend transformation
/// </summary>
/// <typeparam name="T">The chart data type (PieChartData, TimeSeriesData, etc.)</typeparam>
public class ApexChartResponse<T>
{
    /// <summary>
    /// Basic chart configuration
    /// </summary>
    public ChartConfiguration Chart { get; set; } = new();

    /// <summary>
    /// Chart-specific data in ApexCharts format
    /// </summary>
    public T Data { get; set; } = default!;

    /// <summary>
    /// Plot-specific options (varies by chart type)
    /// </summary>
    public Dictionary<string, object>? PlotOptions { get; set; }

    /// <summary>
    /// X-axis configuration
    /// </summary>
    public XAxisConfiguration? XAxis { get; set; }

    /// <summary>
    /// Y-axis configuration
    /// </summary>
    public YAxisConfiguration? YAxis { get; set; }

    /// <summary>
    /// Color palette for the chart
    /// </summary>
    public string[]? Colors { get; set; }

    /// <summary>
    /// Stroke configuration (line width, curve type, etc.)
    /// </summary>
    public Dictionary<string, object>? Stroke { get; set; }

    /// <summary>
    /// Tooltip configuration
    /// </summary>
    public Dictionary<string, object>? Tooltip { get; set; }

    /// <summary>
    /// Legend configuration
    /// </summary>
    public Dictionary<string, object>? Legend { get; set; }

    /// <summary>
    /// Data labels configuration
    /// </summary>
    public Dictionary<string, object>? DataLabels { get; set; }

    /// <summary>
    /// Metadata about the chart and data
    /// </summary>
    public ChartMetadata Metadata { get; set; } = new();
}
