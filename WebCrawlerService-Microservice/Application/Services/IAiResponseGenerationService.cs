namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service interface for generating AI-powered responses from crawled data
/// </summary>
public interface IAiResponseGenerationService
{
    /// <summary>
    /// Generate a summary from crawled data based on user prompt
    /// </summary>
    /// <param name="data">Extracted data items</param>
    /// <param name="userPrompt">Original user request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Natural language summary</returns>
    Task<string> GenerateSummaryAsync(
        List<Dictionary<string, object>> data,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze data and suggest appropriate visualization type
    /// </summary>
    /// <param name="data">Extracted data items</param>
    /// <param name="userPrompt">Original user request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visualization suggestion with chart type and configuration</returns>
    Task<VisualizationSuggestion> SuggestVisualizationAsync(
        List<Dictionary<string, object>> data,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate chart.js configuration for data visualization
    /// </summary>
    /// <param name="data">Extracted data items</param>
    /// <param name="chartType">Type of chart (bar, line, pie, scatter, etc.)</param>
    /// <param name="xField">Field name for X-axis</param>
    /// <param name="yField">Field name for Y-axis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chart.js configuration as JSON string</returns>
    Task<string> GenerateChartConfigAsync(
        List<Dictionary<string, object>> data,
        string chartType,
        string xField,
        string yField,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if user prompt is requesting a data visualization
    /// </summary>
    /// <param name="prompt">User's prompt</param>
    /// <returns>True if visualization is requested</returns>
    bool IsVisualizationRequest(string prompt);
}

/// <summary>
/// Visualization suggestion with chart type and configuration
/// </summary>
public class VisualizationSuggestion
{
    /// <summary>
    /// Recommended chart type (bar, line, pie, scatter, radar, etc.)
    /// </summary>
    public string ChartType { get; set; } = string.Empty;

    /// <summary>
    /// Field name for X-axis or labels
    /// </summary>
    public string XField { get; set; } = string.Empty;

    /// <summary>
    /// Field name for Y-axis or values
    /// </summary>
    public string YField { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional fields for multi-series charts
    /// </summary>
    public List<string> AdditionalFields { get; set; } = new();

    /// <summary>
    /// Explanation of why this visualization was suggested
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }
}
