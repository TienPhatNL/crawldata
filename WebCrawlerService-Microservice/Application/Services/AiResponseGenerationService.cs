using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Services.Crawl4AI;

namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for generating AI-powered responses from crawled data
/// Uses Gemini to create summaries and visualization suggestions
/// </summary>
public class AiResponseGenerationService : IAiResponseGenerationService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<AiResponseGenerationService> _logger;

    // Keywords indicating visualization requests
    private static readonly string[] VisualizationKeywords = new[]
    {
        "chart", "graph", "plot", "visualize", "visualization",
        "compare", "comparison", "trend", "distribution",
        "show me", "display", "bar chart", "line chart", "pie chart"
    };

    public AiResponseGenerationService(
        IGeminiService geminiService,
        ILogger<AiResponseGenerationService> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a summary from crawled data based on user prompt
    /// </summary>
    public async Task<string> GenerateSummaryAsync(
        List<Dictionary<string, object>> data,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null || data.Count == 0)
            {
                return "No data was extracted from the crawl.";
            }

            _logger.LogInformation("Generating summary for {ItemCount} items", data.Count);

            // Prepare data summary for the prompt
            var dataSummary = PrepareDataSummary(data, maxItems: 20);

            var prompt = $@"You are a helpful assistant analyzing web crawl results.

USER REQUEST: ""{userPrompt}""

EXTRACTED DATA ({data.Count} items):
{dataSummary}

Please provide a clear, concise summary that:
1. Directly answers the user's request
2. Highlights key insights from the data
3. Uses natural language (not JSON)
4. Keeps the response under 300 words

If the data shows trends, patterns, or interesting findings, mention them.";

            var summary = await _geminiService.GenerateContentAsync(prompt, cancellationToken);
            return summary?.Trim() ?? "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary");
            return $"Error generating summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Analyze data and suggest appropriate visualization type
    /// </summary>
    public async Task<VisualizationSuggestion> SuggestVisualizationAsync(
        List<Dictionary<string, object>> data,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null || data.Count == 0)
            {
                return new VisualizationSuggestion
                {
                    ChartType = "none",
                    Reasoning = "No data available for visualization",
                    Confidence = 0.0
                };
            }

            _logger.LogInformation("Analyzing data for visualization suggestion");

            // Get field names from first item
            var sampleItem = data[0];
            var fieldNames = string.Join(", ", sampleItem.Keys);

            var dataSample = PrepareDataSummary(data, maxItems: 5);

            var prompt = $@"You are a data visualization expert. Analyze the following data and suggest the best chart type.

USER REQUEST: ""{userPrompt}""

AVAILABLE FIELDS: {fieldNames}

DATA SAMPLE (first 5 items):
{dataSample}

Suggest the most appropriate visualization. Respond with ONLY valid JSON in this format:
{{
  ""chartType"": ""bar|line|pie|scatter|radar|doughnut"",
  ""xField"": ""field name for x-axis or labels"",
  ""yField"": ""field name for y-axis or values"",
  ""additionalFields"": [""optional"", ""fields""],
  ""reasoning"": ""why this chart type is appropriate"",
  ""confidence"": 0.95
}}

Chart type guidelines:
- bar: comparing categories or counts
- line: showing trends over time
- pie/doughnut: showing proportions (use when <10 categories)
- scatter: showing correlation between two numeric variables
- radar: comparing multiple metrics across categories";

            var suggestion = await _geminiService.GenerateJsonAsync<VisualizationSuggestion>(
                prompt, cancellationToken);

            if (suggestion != null)
            {
                _logger.LogInformation(
                    "Suggested {ChartType} chart with confidence {Confidence}",
                    suggestion.ChartType, suggestion.Confidence);
                return suggestion;
            }

            // Fallback suggestion
            return new VisualizationSuggestion
            {
                ChartType = "bar",
                XField = sampleItem.Keys.FirstOrDefault() ?? "name",
                YField = sampleItem.Keys.Skip(1).FirstOrDefault() ?? "value",
                Reasoning = "Default bar chart suggestion",
                Confidence = 0.5
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest visualization");
            return new VisualizationSuggestion
            {
                ChartType = "none",
                Reasoning = $"Error: {ex.Message}",
                Confidence = 0.0
            };
        }
    }

    /// <summary>
    /// Generate chart.js configuration for data visualization
    /// </summary>
    public async Task<string> GenerateChartConfigAsync(
        List<Dictionary<string, object>> data,
        string chartType,
        string xField,
        string yField,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null || data.Count == 0)
            {
                return "{}";
            }

            _logger.LogInformation(
                "Generating {ChartType} chart config with x={XField}, y={YField}",
                chartType, xField, yField);

            // Extract data for the specified fields
            var labels = new List<string>();
            var values = new List<object>();

            foreach (var item in data)
            {
                if (item.TryGetValue(xField, out var xValue))
                {
                    labels.Add(xValue?.ToString() ?? "");
                }

                if (item.TryGetValue(yField, out var yValue))
                {
                    values.Add(yValue);
                }
            }

            // Create Chart.js configuration
            var chartConfig = new
            {
                type = chartType.ToLower(),
                data = new
                {
                    labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = yField,
                            data = values,
                            backgroundColor = GenerateColors(values.Count, alpha: 0.7),
                            borderColor = GenerateColors(values.Count, alpha: 1.0),
                            borderWidth = 2
                        }
                    }
                },
                options = new
                {
                    responsive = true,
                    maintainAspectRatio = false,
                    plugins = new
                    {
                        legend = new { display = true },
                        title = new
                        {
                            display = true,
                            text = $"{yField} by {xField}"
                        }
                    },
                    scales = chartType.ToLower() != "pie" && chartType.ToLower() != "doughnut"
                        ? new
                        {
                            y = new { beginAtZero = true }
                        }
                        : null
                }
            };

            var json = JsonSerializer.Serialize(chartConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate chart config");
            return "{}";
        }
    }

    /// <summary>
    /// Determine if user prompt is requesting a data visualization
    /// </summary>
    public bool IsVisualizationRequest(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var lowerPrompt = prompt.ToLowerInvariant();

        return VisualizationKeywords.Any(keyword => lowerPrompt.Contains(keyword));
    }

    /// <summary>
    /// Prepare a concise summary of data for LLM prompts
    /// </summary>
    private string PrepareDataSummary(List<Dictionary<string, object>> data, int maxItems = 10)
    {
        var sb = new StringBuilder();
        var itemsToShow = Math.Min(data.Count, maxItems);

        for (int i = 0; i < itemsToShow; i++)
        {
            sb.AppendLine($"Item {i + 1}:");
            foreach (var kvp in data[i])
            {
                var value = kvp.Value?.ToString() ?? "null";
                // Truncate long values
                if (value.Length > 100)
                {
                    value = value.Substring(0, 100) + "...";
                }
                sb.AppendLine($"  {kvp.Key}: {value}");
            }
            sb.AppendLine();
        }

        if (data.Count > maxItems)
        {
            sb.AppendLine($"... and {data.Count - maxItems} more items");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate visually appealing colors for charts
    /// </summary>
    private string[] GenerateColors(int count, double alpha = 0.7)
    {
        var colors = new[]
        {
            $"rgba(54, 162, 235, {alpha})",   // Blue
            $"rgba(255, 99, 132, {alpha})",   // Red
            $"rgba(75, 192, 192, {alpha})",   // Teal
            $"rgba(255, 206, 86, {alpha})",   // Yellow
            $"rgba(153, 102, 255, {alpha})",  // Purple
            $"rgba(255, 159, 64, {alpha})",   // Orange
            $"rgba(199, 199, 199, {alpha})",  // Grey
            $"rgba(83, 102, 255, {alpha})",   // Indigo
            $"rgba(255, 99, 255, {alpha})",   // Pink
            $"rgba(99, 255, 132, {alpha})"    // Green
        };

        // Repeat colors if we need more than available
        var result = new List<string>();
        for (int i = 0; i < count; i++)
        {
            result.Add(colors[i % colors.Length]);
        }

        return result.ToArray();
    }
}
