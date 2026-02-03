using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebCrawlerService.Application.DTOs.Charts;
using WebCrawlerService.Application.DTOs.DataVisualization;
using WebCrawlerService.Application.Services.DataVisualization;
using WebCrawlerService.Application.Services.Crawl4AI;

namespace WebCrawlerService.Application.Controllers;

/// <summary>
/// Analytics and AI-powered data visualization endpoints
/// Visualizes crawled/extracted data using intelligent chart recommendations
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IDataVisualizationService _visualizationService;
    private readonly ICrawlSummaryService _summaryService;
    private readonly ISmartCrawlerOrchestrationService _smartCrawler;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IDataVisualizationService visualizationService,
        ICrawlSummaryService summaryService,
        ISmartCrawlerOrchestrationService smartCrawler,
        ILogger<AnalyticsController> logger)
    {
        _visualizationService = visualizationService;
        _summaryService = summaryService;
        _smartCrawler = smartCrawler;
        _logger = logger;
    }

    /// <summary>
    /// NEW: Generates summary/charts across MULTIPLE crawl jobs within the same conversation
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/summary")]
    [AllowAnonymous] // ClassroomService internal call
    [ProducesResponseType(typeof(CrawlJobSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationSummary(
        Guid conversationId,
        [FromQuery] string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📥 API Request: GetConversationSummary for {ConversationId}. Prompt: {Prompt}",
                conversationId, prompt ?? "<none>");

            var result = await _smartCrawler.GetConversationSummaryAsync(
                conversationId,
                prompt ?? "Summarize and provide visualization-ready statistics.",
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "No crawl results found for this conversation" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error generating conversation summary for {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to generate conversation summary" });
        }
    }

    /// <summary>
    /// Analyzes extracted data from a crawl job and provides AI-powered insights
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with schema, chart recommendations, and insights</returns>
    [HttpGet("jobs/{jobId}/analyze")]
    [ProducesResponseType(typeof(DataAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeJobData(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Analyzing data for job {JobId}", jobId);

            var result = await _visualizationService.AnalyzeJobDataAsync(jobId, null, cancellationToken);

            if (result.UrlsWithData == 0)
            {
                return NotFound(new { error = "No extracted data found for this job" });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Job {JobId} not found", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing data for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to analyze job data" });
        }
    }

    /// <summary>
    /// Generates a text-first summary of a crawl job backed by stored CrawlResults
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary DTO with insight highlights and chart metadata</returns>
    [HttpGet("jobs/{jobId}/summary")]
    [AllowAnonymous] // Allow internal microservice calls from ClassroomService
    [ProducesResponseType(typeof(CrawlJobSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobSummary(
        Guid jobId,
        [FromQuery] string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📥 API Request: GetJobSummary for {JobId} by User {User}. Prompt: {Prompt}", jobId, User.Identity?.Name ?? "Unknown", prompt ?? "<none>");
            var summary = await _summaryService.GenerateSummaryAsync(jobId, prompt, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ Summary unavailable (InvalidOperation) for job {JobId}", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Error generating summary for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to generate crawl summary" });
        }
    }

    /// <summary>
    /// Generates a specific chart visualization for the crawl job data
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="chartType">Type of chart (pie, bar, line, scatter, histogram, radial, stacked-bar)</param>
    /// <param name="options">Optional visualization customization options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ApexChart-formatted chart data</returns>
    [HttpPost("jobs/{jobId}/visualize/{chartType}")]
    [ProducesResponseType(typeof(ApexChartResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateVisualization(
        Guid jobId,
        string chartType,
        [FromBody] VisualizationOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating {ChartType} visualization for job {JobId}", chartType, jobId);

            var result = await _visualizationService.GenerateVisualizationAsync(
                jobId,
                chartType,
                options,
                cancellationToken
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate visualization for job {JobId}", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid chart type: {ChartType}", chartType);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating {ChartType} visualization for job {JobId}", chartType, jobId);
            return StatusCode(500, new { error = "Failed to generate visualization" });
        }
    }

    /// <summary>
    /// Gets AI-recommended visualizations for a crawl job
    /// Returns multiple charts based on intelligent analysis of the data
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="maxCharts">Maximum number of charts to generate (default 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommended chart visualizations</returns>
    [HttpGet("jobs/{jobId}/recommended-charts")]
    [ProducesResponseType(typeof(List<ApexChartResponse<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendedVisualizations(
        Guid jobId,
        [FromQuery] int maxCharts = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating {MaxCharts} recommended visualizations for job {JobId}", maxCharts, jobId);

            if (maxCharts < 1 || maxCharts > 10)
            {
                return BadRequest(new { error = "maxCharts must be between 1 and 10" });
            }

            var result = await _visualizationService.GenerateRecommendedVisualizationsAsync(
                jobId,
                maxCharts,
                cancellationToken
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate recommendations for job {JobId}", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommended visualizations for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to generate recommended visualizations" });
        }
    }

    /// <summary>
    /// Analyzes data from a specific URL within a crawl job
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="url">The specific URL to analyze (URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result for the specific URL</returns>
    [HttpGet("jobs/{jobId}/analyze-url")]
    [ProducesResponseType(typeof(DataAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeUrlData(
        Guid jobId,
        [FromQuery] string url,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { error = "URL parameter is required" });
            }

            _logger.LogInformation("Analyzing data for URL {Url} in job {JobId}", url, jobId);

            var result = await _visualizationService.AnalyzeUrlDataAsync(jobId, url, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No data found for URL {Url} in job {JobId}", url, jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing URL data for {Url} in job {JobId}", url, jobId);
            return StatusCode(500, new { error = "Failed to analyze URL data" });
        }
    }

    /// <summary>
    /// Generates a comparison visualization across multiple URLs in a job
    /// Useful for comparing prices, ratings, or other metrics across different sources
    /// </summary>
    /// <param name="jobId">The crawl job ID</param>
    /// <param name="comparisonField">Field to compare (e.g., "price", "rating")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparative bar chart visualization</returns>
    [HttpGet("jobs/{jobId}/compare")]
    [ProducesResponseType(typeof(ApexChartResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateComparisonVisualization(
        Guid jobId,
        [FromQuery] string comparisonField,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(comparisonField))
            {
                return BadRequest(new { error = "comparisonField parameter is required" });
            }

            _logger.LogInformation("Generating comparison visualization for field {Field} in job {JobId}", comparisonField, jobId);

            var result = await _visualizationService.GenerateComparisonVisualizationAsync(
                jobId,
                comparisonField,
                cancellationToken
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate comparison for job {JobId}", jobId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comparison visualization for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to generate comparison visualization" });
        }
    }

    /// <summary>
    /// Get available chart types and their descriptions
    /// </summary>
    /// <returns>List of supported chart types with usage descriptions</returns>
    [HttpGet("chart-types")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ChartTypeInfo>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedChartTypes()
    {
        var chartTypes = new List<ChartTypeInfo>
        {
            new ChartTypeInfo
            {
                Type = "pie",
                Name = "Pie Chart",
                Description = "Shows distribution of categorical data as percentage slices",
                BestFor = "Category distribution, market share, composition",
                RequiredFields = new[] { "One categorical field" }
            },
            new ChartTypeInfo
            {
                Type = "bar",
                Name = "Bar Chart",
                Description = "Compares values across categories using horizontal or vertical bars",
                BestFor = "Comparing quantities, rankings, top N items",
                RequiredFields = new[] { "One categorical field", "One numeric field" }
            },
            new ChartTypeInfo
            {
                Type = "line",
                Name = "Line Chart",
                Description = "Shows trends over time or continuous data",
                BestFor = "Time series, trends, continuous data progression",
                RequiredFields = new[] { "One date/category field", "One numeric field" }
            },
            new ChartTypeInfo
            {
                Type = "scatter",
                Name = "Scatter Plot",
                Description = "Shows relationship between two numeric variables",
                BestFor = "Correlation analysis, outlier detection, pattern recognition",
                RequiredFields = new[] { "Two numeric fields" }
            },
            new ChartTypeInfo
            {
                Type = "histogram",
                Name = "Histogram",
                Description = "Shows frequency distribution of numeric data",
                BestFor = "Data distribution, identifying peaks and ranges",
                RequiredFields = new[] { "One numeric field" }
            },
            new ChartTypeInfo
            {
                Type = "radial",
                Name = "Radial Bar Gauge",
                Description = "Shows single percentage or rating value as a circular gauge",
                BestFor = "Progress, completion rate, average rating, KPIs",
                RequiredFields = new[] { "One percentage/rating field" }
            },
            new ChartTypeInfo
            {
                Type = "stacked-bar",
                Name = "Stacked Bar Chart",
                Description = "Compares multiple values stacked within categories",
                BestFor = "Part-to-whole comparison across categories, composition over groups",
                RequiredFields = new[] { "One categorical field", "One numeric field", "One grouping field" }
            }
        };

        return Ok(chartTypes);
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in claims");
    }
}

/// <summary>
/// Information about a chart type
/// </summary>
public class ChartTypeInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BestFor { get; set; } = string.Empty;
    public string[] RequiredFields { get; set; } = Array.Empty<string>();
}
