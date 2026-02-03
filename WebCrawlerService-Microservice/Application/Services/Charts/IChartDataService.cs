using WebCrawlerService.Application.DTOs.Charts;

namespace WebCrawlerService.Application.Services.Charts;

/// <summary>
/// Service for generating chart-ready data in ApexCharts format
/// </summary>
public interface IChartDataService
{
    /// <summary>
    /// Get job status distribution (pie/donut chart)
    /// Shows breakdown of jobs by status (Completed, Running, Failed, etc.)
    /// </summary>
    Task<ApexChartResponse<PieChartData>> GetJobStatusOverviewAsync(ChartQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get job performance timeline (line/area chart)
    /// Shows job completion rates, success rates over time
    /// </summary>
    Task<ApexChartResponse<TimeSeriesData>> GetJobPerformanceTimelineAsync(TimelineQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get success rate gauge (radial bar chart)
    /// Shows overall success rate as percentage
    /// </summary>
    Task<ApexChartResponse<RadialBarData>> GetSuccessRateGaugeAsync(ChartQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get response time distribution (bar chart)
    /// Shows histogram of response times across all crawls
    /// </summary>
    Task<ApexChartResponse<BarChartData>> GetResponseTimeDistributionAsync(DistributionQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top domains by crawl count (horizontal bar chart)
    /// Shows most frequently crawled domains
    /// </summary>
    Task<ApexChartResponse<BarChartData>> GetTopDomainsAsync(TopNQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get extraction confidence over time (line chart)
    /// Shows AI extraction confidence scores timeline
    /// </summary>
    Task<ApexChartResponse<TimeSeriesData>> GetExtractionConfidenceAsync(TimelineQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system health metrics (mixed chart)
    /// Shows CPU, memory, active jobs, queue length
    /// </summary>
    Task<ApexChartResponse<MultiSeriesData>> GetSystemHealthMultiAsync(TimelineQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cost breakdown by crawler type (stacked bar chart)
    /// Shows cost distribution across different crawler types
    /// </summary>
    Task<ApexChartResponse<StackedBarData>> GetCostBreakdownAsync(TimelineQueryParams queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user quota status (radial bar chart)
    /// Shows current quota usage vs limit
    /// </summary>
    Task<ApexChartResponse<RadialBarData>> GetUserQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get live job progress (real-time radial bar chart)
    /// Shows current job progress with URL completion status
    /// </summary>
    Task<ApexChartResponse<RadialBarData>> GetJobLiveProgressAsync(Guid jobId, CancellationToken cancellationToken = default);
}
