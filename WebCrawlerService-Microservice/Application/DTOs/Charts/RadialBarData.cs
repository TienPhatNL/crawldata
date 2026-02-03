namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for radial bar charts (gauge/progress)
/// Matches ApexCharts radialBar format
/// </summary>
public class RadialBarData
{
    /// <summary>
    /// Series values (percentages 0-100)
    /// </summary>
    public double[] Series { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Labels for each radial bar
    /// </summary>
    public string[] Labels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional target value
    /// </summary>
    public double? Target { get; set; }

    /// <summary>
    /// Optional current value (if different from series percentage)
    /// </summary>
    public double? Current { get; set; }

    /// <summary>
    /// Optional unit (e.g., "URLs", "GB", "requests")
    /// </summary>
    public string? Unit { get; set; }
}
