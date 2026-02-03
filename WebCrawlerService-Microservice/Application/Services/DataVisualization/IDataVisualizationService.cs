using WebCrawlerService.Application.DTOs.Charts;
using WebCrawlerService.Application.DTOs.DataVisualization;

namespace WebCrawlerService.Application.Services.DataVisualization;

/// <summary>
/// Service for AI-powered data visualization and chart generation from crawled data
/// </summary>
public interface IDataVisualizationService
{
    /// <summary>
    /// Analyzes the extracted data from a crawl job and provides AI-powered insights
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="prompt">Optional user prompt to guide analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with schema, recommendations, and insights</returns>
    Task<DataAnalysisResult> AnalyzeJobDataAsync(Guid jobId, string? prompt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a specific chart visualization for the crawl job data
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="chartType">Type of chart to generate (pie, bar, line, etc.)</param>
    /// <param name="options">Optional visualization customization options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ApexChart-formatted chart data</returns>
    Task<ApexChartResponse<object>> GenerateVisualizationAsync(
        Guid jobId,
        string chartType,
        VisualizationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates multiple recommended visualizations based on AI analysis
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="maxCharts">Maximum number of charts to generate (default 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommended chart visualizations</returns>
    Task<List<ApexChartResponse<object>>> GenerateRecommendedVisualizationsAsync(
        Guid jobId,
        int maxCharts = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes data from a specific URL result within a job
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="url">The specific URL to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result for the specific URL</returns>
    Task<DataAnalysisResult> AnalyzeUrlDataAsync(
        Guid jobId,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares data across multiple URLs in a job (e.g., price comparison, trend analysis)
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="comparisonField">Field to compare (e.g., "price", "rating")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparative visualization</returns>
    Task<ApexChartResponse<object>> GenerateComparisonVisualizationAsync(
        Guid jobId,
        string comparisonField,
        CancellationToken cancellationToken = default);
}
