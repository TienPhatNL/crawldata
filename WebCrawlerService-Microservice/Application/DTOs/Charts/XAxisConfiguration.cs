namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// X-axis configuration for ApexCharts
/// </summary>
public class XAxisConfiguration
{
    /// <summary>
    /// X-axis type (e.g., "category", "datetime", "numeric")
    /// </summary>
    public string Type { get; set; } = "category";

    /// <summary>
    /// X-axis label/title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Categories for categorical data
    /// </summary>
    public string[]? Categories { get; set; }

    /// <summary>
    /// Tick amount for datetime axes
    /// </summary>
    public int? TickAmount { get; set; }

    /// <summary>
    /// Min value for numeric axes
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Max value for numeric axes
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Labels configuration
    /// </summary>
    public XAxisLabelsConfiguration? Labels { get; set; }
}

/// <summary>
/// X-axis labels configuration
/// </summary>
public class XAxisLabelsConfiguration
{
    /// <summary>
    /// Whether to rotate labels
    /// </summary>
    public int? Rotate { get; set; }

    /// <summary>
    /// Whether to trim labels
    /// </summary>
    public bool? Trim { get; set; }

    /// <summary>
    /// Maximum width before trimming
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Date format for datetime axes (e.g., "dd MMM", "HH:mm")
    /// </summary>
    public string? Format { get; set; }
}
