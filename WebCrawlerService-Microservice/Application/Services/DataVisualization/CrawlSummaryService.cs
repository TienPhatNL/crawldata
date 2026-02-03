using System.Text;
using System.Text.Json;
using WebCrawlerService.Application.DTOs.DataVisualization;
using System.Linq;

namespace WebCrawlerService.Application.Services.DataVisualization;

/// <summary>
/// Builds text-first summaries for crawl jobs relying on persisted CrawlResults.
/// </summary>
public class CrawlSummaryService : ICrawlSummaryService
{
    private readonly IDataVisualizationService _visualizationService;
    private readonly ILogger<CrawlSummaryService> _logger;

    public CrawlSummaryService(
        IDataVisualizationService visualizationService,
        ILogger<CrawlSummaryService> logger)
    {
        _visualizationService = visualizationService;
        _logger = logger;
    }

    public async Task<CrawlJobSummaryDto> GenerateSummaryAsync(Guid jobId, string? prompt = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating crawl summary for job {JobId} with prompt: {Prompt}", jobId, prompt ?? "<none>");

        try 
        {
            var analysis = await _visualizationService.AnalyzeJobDataAsync(jobId, prompt, cancellationToken);

            if (analysis.UrlsWithData == 0 || analysis.Schema.RecordCount == 0)
            {
                _logger.LogWarning("⚠️ Job {JobId} analysis complete but NO DATA found. UrlsWithData: {Urls}, Records: {Records}", 
                    jobId, analysis.UrlsWithData, analysis.Schema.RecordCount);

                return new CrawlJobSummaryDto
                {
                    JobId = jobId,
                    DataDomain = analysis.DataDomain,
                    SummaryText = "This crawl job has not produced any structured data yet.",
                    TotalRecords = 0,
                    UrlsWithData = analysis.UrlsWithData,
                    GeneratedAt = DateTime.UtcNow,
                    InsightHighlights = analysis.Warnings.Any()
                        ? analysis.Warnings
                        : new List<string> { "No extracted rows were stored for this job." }
                };
            }

            var topFields = analysis.Schema.Fields
                .OrderByDescending(f => f.Coverage)
                .ThenBy(f => f.Name)
                .Take(5)
                .Select(f => new FieldCoverageDto
                {
                    Name = f.Name,
                    DataType = string.IsNullOrWhiteSpace(f.SemanticType) ? f.DataType : f.SemanticType,
                    CoveragePercent = Math.Round(f.Coverage, 2),
                    SampleValues = ExtractSampleValues(f.SampleValues)
                })
                .ToList();

            var insightHighlights = BuildInsightHighlights(analysis, topFields);
            
            // Use AI summary if available, otherwise fallback to narrative builder
            var summaryText = !string.IsNullOrWhiteSpace(analysis.Summary) 
                ? analysis.Summary 
                : BuildNarrativeSummary(analysis, topFields, insightHighlights);

            var chartPreviews = BuildChartPreviews(jobId, analysis.ChartRecommendations);

            // Populate data for the first chart to allow immediate rendering
            if (chartPreviews.Count > 0)
            {
                try
                {
                    var topChart = chartPreviews[0];
                    
                    // If custom prompt provided, always regenerate chart to match the prompt
                    // Otherwise, reuse pre-generated chart data if available
                    bool shouldRegenerate = !string.IsNullOrWhiteSpace(prompt) || topChart.ChartData == null;
                    
                    if (shouldRegenerate)
                    {
                        _logger.LogInformation(prompt != null 
                            ? "Regenerating chart for custom prompt: {Prompt}" 
                            : "No pre-generated data for {Title}, triggering local generation...", 
                            prompt ?? topChart.Title);

                        var visualizationOptions = new VisualizationOptions
                        {
                            Title = topChart.Title,
                            XAxisField = topChart.XAxisField,
                            YAxisField = topChart.YAxisField,
                            AggregationFunction = topChart.Aggregation
                        };

                        var chartData = await _visualizationService.GenerateVisualizationAsync(
                            jobId, 
                            topChart.ChartType, 
                            visualizationOptions,
                            cancellationToken: cancellationToken);

                        topChart.ChartData = chartData;
                    }
                    else
                    {
                        _logger.LogInformation("Using pre-generated chart data for '{Title}'", topChart.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-fetch chart data for summary");
                }
            }
            
            return new CrawlJobSummaryDto
            {
                JobId = jobId,
                DataDomain = string.IsNullOrWhiteSpace(analysis.DataDomain) ? "General" : analysis.DataDomain,
                SummaryText = summaryText,
                TotalRecords = analysis.Schema.RecordCount,
                UrlsWithData = analysis.UrlsWithData,
                TopFields = topFields,
                InsightHighlights = insightHighlights,
                ChartPreviews = chartPreviews,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Fatal error inside GenerateSummaryAsync for Job {JobId}", jobId);
            throw;
        }
    }

    private static IReadOnlyList<string> ExtractSampleValues(IEnumerable<object> sampleValues)
    {
        return sampleValues
            .Where(v => v != null)
            .Select(FormatSampleValue)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .Take(3)
            .ToList();
    }

    private static string FormatSampleValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            System.Text.Json.JsonElement json => json.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => json.GetString() ?? string.Empty,
                System.Text.Json.JsonValueKind.Number => json.TryGetDouble(out var dbl) ? dbl.ToString("0.##") : json.ToString(),
                System.Text.Json.JsonValueKind.True => "true",
                System.Text.Json.JsonValueKind.False => "false",
                _ => json.ToString()
            },
            _ => value.ToString() ?? string.Empty
        };
    }

    private static IReadOnlyList<string> BuildInsightHighlights(
        DataAnalysisResult analysis,
        IReadOnlyList<FieldCoverageDto> topFields)
    {
        var highlights = new List<string>
        {
            $"Parsed {analysis.Schema.RecordCount} rows across {analysis.UrlsWithData} URLs."
        };

        if (topFields.Count > 0)
        {
            highlights.Add($"Most common field '{topFields[0].Name}' appears in {topFields[0].CoveragePercent:0.##}% of records.");
        }

        highlights.AddRange(analysis.Insights.Take(3));

        return highlights;
    }

    private static string BuildNarrativeSummary(
        DataAnalysisResult analysis,
        IReadOnlyList<FieldCoverageDto> topFields,
        IReadOnlyList<string> insightHighlights)
    {
        var builder = new StringBuilder();
        builder.Append($"{analysis.Schema.RecordCount} entries were stored from {analysis.UrlsWithData} crawled URLs");
        if (!string.IsNullOrWhiteSpace(analysis.DataDomain))
        {
            builder.Append($" in the {analysis.DataDomain} domain");
        }
        builder.Append('.')
               .Append(' ');

        if (topFields.Count > 0)
        {
            var fieldDescriptions = topFields
                .Select(f => $"{f.Name} ({f.CoveragePercent:0.#}% coverage)")
                .ToList();
            builder.Append("Key fields include ")
                   .Append(string.Join(", ", fieldDescriptions))
                   .Append('.')
                   .Append(' ');
        }

        if (insightHighlights.Count > 0)
        {
            builder.Append(insightHighlights.First());
        }

        return builder.ToString();
    }

    private static IReadOnlyList<ChartPreviewDto> BuildChartPreviews(
        Guid jobId,
        IEnumerable<ChartRecommendation> recommendations)
    {
        if (recommendations == null)
        {
            return Array.Empty<ChartPreviewDto>();
        }

        return recommendations
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.Confidence)
            .Take(3)
            .Select(r => new ChartPreviewDto
            {
                ChartType = r.ChartType,
                Title = r.Title,
                Reasoning = r.Reasoning,
                Confidence = r.Confidence,
                ExpectedInsights = r.ExpectedInsights.Take(3).ToList(),
                XAxisField = r.XAxisFields.FirstOrDefault(),
                YAxisField = r.YAxisFields.FirstOrDefault(),
                Aggregation = r.AggregationFunction,
                FetchUrl = $"/api/analytics/jobs/{jobId}/visualize/{r.ChartType.ToLowerInvariant()}",
                ChartData = r.PreGeneratedData // Ensure data is copied during preview construction
            })
            .ToList();
    }
}
