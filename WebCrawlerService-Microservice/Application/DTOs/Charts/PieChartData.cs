namespace WebCrawlerService.Application.DTOs.Charts;

/// <summary>
/// Data structure for pie and donut charts
/// Matches ApexCharts pie/donut series and labels format
/// </summary>
public class PieChartData
{
    /// <summary>
    /// Series values (numbers for each slice)
    /// </summary>
    public double[] Series { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Labels for each slice
    /// </summary>
    public string[] Labels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional total value for display in donut center
    /// </summary>
    public double? Total { get; set; }

    /// <summary>
    /// Optional percentage calculations
    /// </summary>
    public double[]? Percentages { get; set; }
}
