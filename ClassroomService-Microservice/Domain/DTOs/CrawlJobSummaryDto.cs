using System.Text.Json.Serialization;

namespace ClassroomService.Domain.DTOs;

public class CrawlJobSummaryDto
{
    public Guid JobId { get; set; }
    public string DataDomain { get; set; } = string.Empty;
    public string SummaryText { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int UrlsWithData { get; set; }
    public List<string> InsightHighlights { get; set; } = new();
    public List<ChartPreviewDto> ChartPreviews { get; set; } = new();
}

public class ChartPreviewDto
{
    public string ChartType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public object? ChartData { get; set; }
}
