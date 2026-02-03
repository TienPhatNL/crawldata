using System.Collections.ObjectModel;

namespace WebCrawlerService.Application.DTOs.DataVisualization;

/// <summary>
/// Lightweight summary of a crawl job that can be rendered directly in Classroom or dashboards.
/// </summary>
public class CrawlJobSummaryDto
{
    public Guid JobId { get; set; }
    public string DataDomain { get; set; } = string.Empty;
    public string SummaryText { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int UrlsWithData { get; set; }
    public IReadOnlyList<FieldCoverageDto> TopFields { get; set; } = ReadOnlyCollection<FieldCoverageDto>.Empty;
    public IReadOnlyList<string> InsightHighlights { get; set; } = ReadOnlyCollection<string>.Empty;
    public IReadOnlyList<ChartPreviewDto> ChartPreviews { get; set; } = ReadOnlyCollection<ChartPreviewDto>.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Coverage details for a frequently occurring extracted field.
/// </summary>
public class FieldCoverageDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public double CoveragePercent { get; set; }
    public IReadOnlyList<string> SampleValues { get; set; } = ReadOnlyCollection<string>.Empty;
}

/// <summary>
/// Metadata describing a chart that clients can fetch and render via ApexCharts when needed.
/// </summary>
public class ChartPreviewDto
{
    public string ChartType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public IReadOnlyList<string> ExpectedInsights { get; set; } = ReadOnlyCollection<string>.Empty;
    public string? XAxisField { get; set; }
    public string? YAxisField { get; set; }
    public string? Aggregation { get; set; }
    public string FetchUrl { get; set; } = string.Empty;
    public object? ChartData { get; set; }
}
