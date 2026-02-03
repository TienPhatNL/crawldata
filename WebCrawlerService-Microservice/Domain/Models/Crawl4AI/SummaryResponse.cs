using System.Text.Json.Serialization;

namespace WebCrawlerService.Domain.Models.Crawl4AI;

public class SummaryResponse
{
    [JsonPropertyName("summaryText")]
    public string SummaryText { get; set; } = string.Empty;

    [JsonPropertyName("insightHighlights")]
    public List<string> InsightHighlights { get; set; } = new();

    [JsonPropertyName("fieldCoverage")]
    public List<FieldCoverage> FieldCoverage { get; set; } = new();

    [JsonPropertyName("charts")]
    public List<ChartRecommendationModel> Charts { get; set; } = new();
}

public class FieldCoverage
{
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;

    [JsonPropertyName("coveragePercent")]
    public double CoveragePercent { get; set; }
}

public class ChartRecommendationModel
{
    [JsonPropertyName("chartType")]
    public string ChartType { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("expectedInsights")]
    public List<string> ExpectedInsights { get; set; } = new();

    [JsonPropertyName("xAxisFields")]
    public List<string> XAxisFields { get; set; } = new();

    [JsonPropertyName("yAxisFields")]
    public List<string> YAxisFields { get; set; } = new();

    [JsonPropertyName("aggregationFunction")]
    public string AggregationFunction { get; set; } = string.Empty;

    [JsonPropertyName("chartData")]
    public object? PreGeneratedData { get; set; }
}

