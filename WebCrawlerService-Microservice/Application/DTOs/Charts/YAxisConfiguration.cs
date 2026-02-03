namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Y-axis configuration for ApexCharts
/// </summary>
public class YAxisConfiguration
{
    /// <summary>
    /// Y-axis label/title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Min value
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Max value
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Tick amount
    /// </summary>
    public int? TickAmount { get; set; }

    /// <summary>
    /// Whether to show opposite axis
    /// </summary>
    public bool? Opposite { get; set; }

    /// <summary>
    /// Label formatter function name or pattern
    /// </summary>
    public string? LabelFormatter { get; set; }

    /// <summary>
    /// Labels configuration
    /// </summary>
    public YAxisLabelsConfiguration? Labels { get; set; }
}

/// <summary>
/// Y-axis labels configuration
/// </summary>
public class YAxisLabelsConfiguration
{
    /// <summary>
    /// Number format (e.g., "#,###", "0.00")
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Minimum width for labels
    /// </summary>
    public int? MinWidth { get; set; }

    /// <summary>
    /// Maximum width for labels
    /// </summary>
    public int? MaxWidth { get; set; }
}
