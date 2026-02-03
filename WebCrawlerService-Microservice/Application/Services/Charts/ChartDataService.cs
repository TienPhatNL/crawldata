using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebCrawlerService.Application.DTOs.Charts;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Infrastructure.Contexts;

namespace WebCrawlerService.Application.Services.Charts;

public class ChartDataService : IChartDataService
{
    private readonly CrawlerDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IUserQuotaService _quotaService;
    private readonly ILogger<ChartDataService> _logger;

    // Color palettes for different chart types
    private readonly string[] _statusColors = new[] { "#00E396", "#008FFB", "#FEB019", "#FF4560", "#775DD0" };
    private readonly string[] _performanceColors = new[] { "#00E396", "#FF4560", "#775DD0" };
    private readonly string[] _systemColors = new[] { "#008FFB", "#00E396", "#FEB019", "#FF4560" };

    public ChartDataService(
        CrawlerDbContext context,
        IDistributedCache cache,
        IUserQuotaService quotaService,
        ILogger<ChartDataService> logger)
    {
        _context = context;
        _cache = cache;
        _quotaService = quotaService;
        _logger = logger;
    }

    public async Task<ApexChartResponse<PieChartData>> GetJobStatusOverviewAsync(
        ChartQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:job-status:{queryParams.UserId}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<PieChartData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var query = _context.CrawlJobs.AsQueryable();
        query = ApplyBaseFilters(query, queryParams);

        var statusGroups = await query
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync(cancellationToken);

        var labels = statusGroups.Select(g => g.Status.ToString()).ToArray();
        var series = statusGroups.Select(g => (double)g.Count).ToArray();
        var total = series.Sum();

        var response = new ApexChartResponse<PieChartData>
        {
            Chart = new ChartConfiguration
            {
                Type = "donut",
                Title = "Job Status Overview",
                Subtitle = $"Total: {total} jobs",
                Height = "350px"
            },
            Data = new PieChartData
            {
                Series = series,
                Labels = labels,
                Total = total,
                Percentages = series.Select(s => Math.Round((s / total) * 100, 2)).ToArray()
            },
            Colors = _statusColors,
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = GetPeriodString(queryParams.StartDate, queryParams.EndDate),
                DataPoints = labels.Length
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<TimeSeriesData>> GetJobPerformanceTimelineAsync(
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:job-performance:{queryParams.UserId}:{queryParams.Interval}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<TimeSeriesData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var startDate = queryParams.StartDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = queryParams.EndDate ?? DateTime.UtcNow;
        var interval = queryParams.Interval ?? DetermineInterval(startDate, endDate);

        var query = _context.CrawlJobs
            .Where(j => j.CreatedAt >= startDate && j.CreatedAt <= endDate);

        if (queryParams.UserId.HasValue)
            query = query.Where(j => j.UserId == queryParams.UserId.Value);

        var jobs = await query.ToListAsync(cancellationToken);

        var groupedData = GroupByInterval(jobs, j => j.CreatedAt, interval);

        var completedSeries = new TimeSeriesItem
        {
            Name = "Completed Jobs",
            Data = groupedData.Select(g => new object[]
            {
                new DateTimeOffset(g.Key).ToUnixTimeMilliseconds(),
                g.Value.Count(j => j.Status == JobStatus.Completed)
            }).ToList()
        };

        var failedSeries = new TimeSeriesItem
        {
            Name = "Failed Jobs",
            Data = groupedData.Select(g => new object[]
            {
                new DateTimeOffset(g.Key).ToUnixTimeMilliseconds(),
                g.Value.Count(j => j.Status == JobStatus.Failed)
            }).ToList()
        };

        var runningSeries = new TimeSeriesItem
        {
            Name = "Running Jobs",
            Data = groupedData.Select(g => new object[]
            {
                new DateTimeOffset(g.Key).ToUnixTimeMilliseconds(),
                g.Value.Count(j => j.Status == JobStatus.Running)
            }).ToList()
        };

        var response = new ApexChartResponse<TimeSeriesData>
        {
            Chart = new ChartConfiguration
            {
                Type = "area",
                Title = "Job Performance Timeline",
                Subtitle = $"Jobs from {startDate:MMM dd} to {endDate:MMM dd}",
                Height = "400px",
                Stacked = false
            },
            Data = new TimeSeriesData
            {
                Series = new List<TimeSeriesItem> { completedSeries, failedSeries, runningSeries }
            },
            Colors = _performanceColors,
            XAxis = new XAxisConfiguration
            {
                Type = "datetime",
                Title = "Time"
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Number of Jobs",
                Min = 0
            },
            Stroke = new Dictionary<string, object>
            {
                { "curve", "smooth" },
                { "width", 2 }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                DataPoints = groupedData.Count,
                AdditionalInfo = new Dictionary<string, object>
                {
                    { "interval", interval }
                }
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<RadialBarData>> GetSuccessRateGaugeAsync(
        ChartQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:success-rate:{queryParams.UserId}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<RadialBarData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var query = _context.CrawlJobs.AsQueryable();
        query = ApplyBaseFilters(query, queryParams);

        var totalJobs = await query.CountAsync(cancellationToken);
        var successfulJobs = await query.CountAsync(j => j.Status == JobStatus.Completed, cancellationToken);

        var successRate = totalJobs > 0 ? Math.Round((double)successfulJobs / totalJobs * 100, 2) : 0;

        var response = new ApexChartResponse<RadialBarData>
        {
            Chart = new ChartConfiguration
            {
                Type = "radialBar",
                Title = "Overall Success Rate",
                Subtitle = $"{successfulJobs} of {totalJobs} jobs completed",
                Height = "350px"
            },
            Data = new RadialBarData
            {
                Series = new[] { successRate },
                Labels = new[] { "Success Rate" },
                Current = successfulJobs,
                Target = totalJobs,
                Unit = "jobs"
            },
            Colors = new[] { successRate >= 80 ? "#00E396" : successRate >= 50 ? "#FEB019" : "#FF4560" },
            PlotOptions = new Dictionary<string, object>
            {
                { "radialBar", new Dictionary<string, object>
                    {
                        { "hollow", new Dictionary<string, object> { { "size", "70%" } } },
                        { "dataLabels", new Dictionary<string, object>
                            {
                                { "name", new Dictionary<string, object> { { "fontSize", "22px" } } },
                                { "value", new Dictionary<string, object> { { "fontSize", "16px" } } }
                            }
                        }
                    }
                }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = GetPeriodString(queryParams.StartDate, queryParams.EndDate),
                DataPoints = 1
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<BarChartData>> GetResponseTimeDistributionAsync(
        DistributionQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:response-time:{queryParams.UserId}:{queryParams.BucketCount}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<BarChartData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var query = _context.CrawlResults.AsQueryable();

        if (queryParams.StartDate.HasValue)
            query = query.Where(r => r.CrawledAt >= queryParams.StartDate.Value);
        if (queryParams.EndDate.HasValue)
            query = query.Where(r => r.CrawledAt <= queryParams.EndDate.Value);

        var responseTimes = await query
            .Where(r => r.ResponseTimeMs > 0)
            .Select(r => r.ResponseTimeMs)
            .ToListAsync(cancellationToken);

        if (!responseTimes.Any())
        {
            return CreateEmptyBarChart("Response Time Distribution", "No data available");
        }

        var buckets = CreateDistributionBuckets(responseTimes, queryParams.BucketCount);

        var response = new ApexChartResponse<BarChartData>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = "Response Time Distribution",
                Subtitle = $"Based on {responseTimes.Count} crawl results",
                Height = "400px"
            },
            Data = new BarChartData
            {
                Series = new List<BarSeriesItem>
                {
                    new BarSeriesItem
                    {
                        Name = "URL Count",
                        Data = buckets.Values.Select(v => (double)v).ToArray()
                    }
                },
                Categories = buckets.Keys.ToArray()
            },
            Colors = new[] { "#008FFB" },
            XAxis = new XAxisConfiguration
            {
                Type = "category",
                Title = "Response Time (ms)",
                Labels = new XAxisLabelsConfiguration { Rotate = -45 }
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Number of URLs",
                Min = 0
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = GetPeriodString(queryParams.StartDate, queryParams.EndDate),
                DataPoints = buckets.Count
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<BarChartData>> GetTopDomainsAsync(
        TopNQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:top-domains:{queryParams.UserId}:{queryParams.TopN}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<BarChartData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var query = _context.CrawlResults.AsQueryable();

        if (queryParams.StartDate.HasValue)
            query = query.Where(r => r.CrawledAt >= queryParams.StartDate.Value);
        if (queryParams.EndDate.HasValue)
            query = query.Where(r => r.CrawledAt <= queryParams.EndDate.Value);

        var results = await query
            .Where(r => !string.IsNullOrEmpty(r.Url))
            .ToListAsync(cancellationToken);

        var domainGroups = results
            .GroupBy(r => ExtractDomain(r.Url))
            .Select(g => new { Domain = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(queryParams.TopN)
            .ToList();

        if (!domainGroups.Any())
        {
            return CreateEmptyBarChart("Top Crawled Domains", "No data available");
        }

        var response = new ApexChartResponse<BarChartData>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = $"Top {queryParams.TopN} Crawled Domains",
                Subtitle = $"Most frequently crawled domains",
                Height = "400px",
                Horizontal = true
            },
            Data = new BarChartData
            {
                Series = new List<BarSeriesItem>
                {
                    new BarSeriesItem
                    {
                        Name = "Crawl Count",
                        Data = domainGroups.Select(g => (double)g.Count).ToArray()
                    }
                },
                Categories = domainGroups.Select(g => g.Domain).ToArray()
            },
            Colors = new[] { "#00E396" },
            XAxis = new XAxisConfiguration
            {
                Type = "category",
                Title = "Number of Crawls"
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Domain"
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = GetPeriodString(queryParams.StartDate, queryParams.EndDate),
                DataPoints = domainGroups.Count
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<TimeSeriesData>> GetExtractionConfidenceAsync(
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:extraction-confidence:{queryParams.UserId}:{queryParams.Interval}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<TimeSeriesData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var startDate = queryParams.StartDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = queryParams.EndDate ?? DateTime.UtcNow;
        var interval = queryParams.Interval ?? DetermineInterval(startDate, endDate);

        var results = await _context.CrawlResults
            .Where(r => r.CrawledAt >= startDate && r.CrawledAt <= endDate)
            .Where(r => r.ExtractedDataJson != null)
            .ToListAsync(cancellationToken);

        var groupedData = GroupByInterval(results, r => r.CrawledAt, interval);

        var avgConfidenceSeries = new TimeSeriesItem
        {
            Name = "Avg Confidence Score",
            Data = groupedData.Select(g => new object[]
            {
                new DateTimeOffset(g.Key).ToUnixTimeMilliseconds(),
                Math.Round(g.Value.Average(r => ExtractConfidenceScore(r.ExtractedDataJson)), 2)
            }).ToList()
        };

        var response = new ApexChartResponse<TimeSeriesData>
        {
            Chart = new ChartConfiguration
            {
                Type = "line",
                Title = "AI Extraction Confidence Over Time",
                Subtitle = "Average confidence scores from AI extraction",
                Height = "350px"
            },
            Data = new TimeSeriesData
            {
                Series = new List<TimeSeriesItem> { avgConfidenceSeries }
            },
            Colors = new[] { "#775DD0" },
            XAxis = new XAxisConfiguration
            {
                Type = "datetime",
                Title = "Time"
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Confidence Score",
                Min = 0,
                Max = 100
            },
            Stroke = new Dictionary<string, object>
            {
                { "curve", queryParams.Smooth ? "smooth" : "straight" },
                { "width", 3 }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                DataPoints = groupedData.Count
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<MultiSeriesData>> GetSystemHealthMultiAsync(
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:system-health:{queryParams.Interval}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<MultiSeriesData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var startDate = queryParams.StartDate ?? DateTime.UtcNow.AddHours(-1);
        var endDate = queryParams.EndDate ?? DateTime.UtcNow;

        var jobs = await _context.CrawlJobs
            .Where(j => j.CreatedAt >= startDate && j.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var interval = queryParams.Interval ?? "minute";
        var groupedData = GroupByInterval(jobs, j => j.CreatedAt, interval);

        var categories = groupedData.Select(g => g.Key.ToString("HH:mm")).ToArray();

        var activeJobsSeries = new MultiSeriesItem
        {
            Name = "Active Jobs",
            Type = "column",
            Data = groupedData.Select(g => (double)g.Value.Count(j => j.Status == JobStatus.Running)).ToArray()
        };

        var queuedJobsSeries = new MultiSeriesItem
        {
            Name = "Queued Jobs",
            Type = "column",
            Data = groupedData.Select(g => (double)g.Value.Count(j => j.Status == JobStatus.Pending)).ToArray()
        };

        var completionRateSeries = new MultiSeriesItem
        {
            Name = "Completion Rate (%)",
            Type = "line",
            Data = groupedData.Select(g =>
            {
                var total = g.Value.Count;
                var completed = g.Value.Count(j => j.Status == JobStatus.Completed);
                return total > 0 ? Math.Round((double)completed / total * 100, 2) : 0;
            }).ToArray()
        };

        var response = new ApexChartResponse<MultiSeriesData>
        {
            Chart = new ChartConfiguration
            {
                Type = "line",
                Title = "System Health Metrics",
                Subtitle = "Real-time system performance",
                Height = "400px"
            },
            Data = new MultiSeriesData
            {
                Series = new List<MultiSeriesItem> { activeJobsSeries, queuedJobsSeries, completionRateSeries },
                Categories = categories
            },
            Colors = _systemColors,
            XAxis = new XAxisConfiguration
            {
                Type = "category",
                Title = "Time",
                Categories = categories
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Count / Percentage"
            },
            Stroke = new Dictionary<string, object>
            {
                { "width", new[] { 0, 0, 4 } },
                { "curve", "smooth" }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:HH:mm} to {endDate:HH:mm}",
                DataPoints = categories.Length
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, 60, cancellationToken); // 1-minute cache for system metrics
        }

        return response;
    }

    public async Task<ApexChartResponse<StackedBarData>> GetCostBreakdownAsync(
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:cost-breakdown:{queryParams.UserId}:{queryParams.Interval}:{queryParams.StartDate}:{queryParams.EndDate}";

        if (queryParams.UseCache)
        {
            var cached = await GetFromCacheAsync<ApexChartResponse<StackedBarData>>(cacheKey, cancellationToken);
            if (cached != null) return cached;
        }

        var startDate = queryParams.StartDate ?? DateTime.UtcNow.AddDays(-7);
        var endDate = queryParams.EndDate ?? DateTime.UtcNow;

        var jobs = await _context.CrawlJobs
            .Where(j => j.CreatedAt >= startDate && j.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var interval = queryParams.Interval ?? DetermineInterval(startDate, endDate);
        var groupedData = GroupByInterval(jobs, j => j.CreatedAt, interval);

        var categories = groupedData.Select(g => g.Key.ToString("MMM dd")).ToArray();

        var crawlerTypes = Enum.GetValues<CrawlerType>();
        var series = crawlerTypes.Select(type => new StackedSeriesItem
        {
            Name = type.ToString(),
            Data = groupedData.Select(g => (double)g.Value.Count(j => j.CrawlerType == type)).ToArray()
        }).ToList();

        var response = new ApexChartResponse<StackedBarData>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = "Job Distribution by Crawler Type",
                Subtitle = "Stacked breakdown of crawler usage",
                Height = "400px",
                Stacked = true
            },
            Data = new StackedBarData
            {
                Series = series,
                Categories = categories,
                Totals = groupedData.Select(g => (double)g.Value.Count).ToArray()
            },
            Colors = _statusColors,
            XAxis = new XAxisConfiguration
            {
                Type = "category",
                Title = "Date",
                Categories = categories
            },
            YAxis = new YAxisConfiguration
            {
                Title = "Number of Jobs"
            },
            PlotOptions = new Dictionary<string, object>
            {
                { "bar", new Dictionary<string, object>
                    {
                        { "horizontal", false }
                    }
                }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                DataPoints = categories.Length
            }
        };

        if (queryParams.UseCache)
        {
            await SetCacheAsync(cacheKey, response, queryParams.CacheTtl, cancellationToken);
        }

        return response;
    }

    public async Task<ApexChartResponse<RadialBarData>> GetUserQuotaStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"chart:user-quota:{userId}";

        var cached = await GetFromCacheAsync<ApexChartResponse<RadialBarData>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var quotaInfo = await _quotaService.GetQuotaInfoAsync(userId, cancellationToken);
        var usedCount = 0;
        var quotaLimit = 0;

        if (quotaInfo != null && quotaInfo.TotalQuota > 0)
        {
            quotaLimit = quotaInfo.TotalQuota;
            usedCount = Math.Max(0, quotaInfo.TotalQuota - quotaInfo.RemainingQuota);
        }
        else
        {
            usedCount = await _context.CrawlJobs
                .Where(j => j.UserId == userId && j.CreatedAt >= startOfMonth)
                .CountAsync(cancellationToken);
            quotaLimit = 100;
        }

        if (quotaLimit <= 0)
        {
            quotaLimit = 1;
        }

        var usagePercentage = Math.Round((double)usedCount / quotaLimit * 100, 2);

        var response = new ApexChartResponse<RadialBarData>
        {
            Chart = new ChartConfiguration
            {
                Type = "radialBar",
                Title = "Monthly Quota Usage",
                Subtitle = $"{usedCount} of {quotaLimit} jobs used",
                Height = "350px"
            },
            Data = new RadialBarData
            {
                Series = new[] { usagePercentage },
                Labels = new[] { "Quota Usage" },
                Current = usedCount,
                Target = quotaLimit,
                Unit = "jobs"
            },
            Colors = new[] { usagePercentage >= 90 ? "#FF4560" : usagePercentage >= 70 ? "#FEB019" : "#00E396" },
            PlotOptions = new Dictionary<string, object>
            {
                { "radialBar", new Dictionary<string, object>
                    {
                        { "hollow", new Dictionary<string, object> { { "size", "70%" } } },
                        { "dataLabels", new Dictionary<string, object>
                            {
                                { "name", new Dictionary<string, object> { { "fontSize", "22px" } } },
                                { "value", new Dictionary<string, object> { { "fontSize", "16px" } } },
                                { "total", new Dictionary<string, object>
                                    {
                                        { "show", true },
                                        { "label", $"{usedCount}/{quotaLimit}" }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startOfMonth:MMMM yyyy}",
                DataPoints = 1
            }
        };

        await SetCacheAsync(cacheKey, response, 60, cancellationToken); // 1-minute cache

        return response;
    }

    public async Task<ApexChartResponse<RadialBarData>> GetJobLiveProgressAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        // No caching for live data
        var job = await _context.CrawlJobs
            .Include(j => j.Results)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        var totalUrls = job.Results.Count;
        var completedUrls = job.Results.Count(r => r.HttpStatusCode >= 200 && r.HttpStatusCode < 300);
        var progressPercentage = totalUrls > 0 ? Math.Round((double)completedUrls / totalUrls * 100, 2) : 0;

        var response = new ApexChartResponse<RadialBarData>
        {
            Chart = new ChartConfiguration
            {
                Type = "radialBar",
                Title = $"Job Progress",
                Subtitle = $"Job ID: {jobId.ToString()[..8]}...",
                Height = "300px"
            },
            Data = new RadialBarData
            {
                Series = new[] { progressPercentage },
                Labels = new[] { "Progress" },
                Current = completedUrls,
                Target = totalUrls,
                Unit = "URLs"
            },
            Colors = new[] { job.Status == JobStatus.Completed ? "#00E396" : job.Status == JobStatus.Failed ? "#FF4560" : "#008FFB" },
            PlotOptions = new Dictionary<string, object>
            {
                { "radialBar", new Dictionary<string, object>
                    {
                        { "hollow", new Dictionary<string, object> { { "size", "65%" } } },
                        { "dataLabels", new Dictionary<string, object>
                            {
                                { "name", new Dictionary<string, object> { { "fontSize", "18px" } } },
                                { "value", new Dictionary<string, object> { { "fontSize", "14px" } } },
                                { "total", new Dictionary<string, object>
                                    {
                                        { "show", true },
                                        { "label", $"{completedUrls}/{totalUrls} URLs" }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                DataPoints = 1,
                AdditionalInfo = new Dictionary<string, object>
                {
                    { "jobStatus", job.Status.ToString() },
                    { "startedAt", job.StartedAt?.ToString("o") ?? "N/A" },
                    { "completedAt", job.CompletedAt?.ToString("o") ?? "N/A" }
                }
            }
        };

        return response;
    }

    // Helper methods

    private IQueryable<Domain.Entities.CrawlJob> ApplyBaseFilters(
        IQueryable<Domain.Entities.CrawlJob> query,
        ChartQueryParams queryParams)
    {
        if (queryParams.UserId.HasValue)
            query = query.Where(j => j.UserId == queryParams.UserId.Value);

        if (queryParams.StartDate.HasValue)
            query = query.Where(j => j.CreatedAt >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(j => j.CreatedAt <= queryParams.EndDate.Value);

        if (!string.IsNullOrEmpty(queryParams.Status) && Enum.TryParse<JobStatus>(queryParams.Status, out var status))
            query = query.Where(j => j.Status == status);

        if (!string.IsNullOrEmpty(queryParams.CrawlerType) && Enum.TryParse<CrawlerType>(queryParams.CrawlerType, out var crawlerType))
            query = query.Where(j => j.CrawlerType == crawlerType);

        return query;
    }

    private Dictionary<DateTime, List<T>> GroupByInterval<T>(
        List<T> items,
        Func<T, DateTime> dateSelector,
        string interval)
    {
        return interval.ToLower() switch
        {
            "minute" => items.GroupBy(i => new DateTime(dateSelector(i).Year, dateSelector(i).Month, dateSelector(i).Day, dateSelector(i).Hour, dateSelector(i).Minute, 0))
                            .ToDictionary(g => g.Key, g => g.ToList()),
            "hour" => items.GroupBy(i => new DateTime(dateSelector(i).Year, dateSelector(i).Month, dateSelector(i).Day, dateSelector(i).Hour, 0, 0))
                          .ToDictionary(g => g.Key, g => g.ToList()),
            "day" => items.GroupBy(i => dateSelector(i).Date)
                         .ToDictionary(g => g.Key, g => g.ToList()),
            "week" => items.GroupBy(i => dateSelector(i).Date.AddDays(-(int)dateSelector(i).DayOfWeek))
                          .ToDictionary(g => g.Key, g => g.ToList()),
            "month" => items.GroupBy(i => new DateTime(dateSelector(i).Year, dateSelector(i).Month, 1))
                           .ToDictionary(g => g.Key, g => g.ToList()),
            _ => items.GroupBy(i => dateSelector(i).Date)
                     .ToDictionary(g => g.Key, g => g.ToList())
        };
    }

    private string DetermineInterval(DateTime startDate, DateTime endDate)
    {
        var duration = endDate - startDate;
        if (duration.TotalHours <= 1) return "minute";
        if (duration.TotalDays <= 1) return "hour";
        if (duration.TotalDays <= 7) return "day";
        if (duration.TotalDays <= 90) return "week";
        return "month";
    }

    private Dictionary<string, int> CreateDistributionBuckets(List<int> values, int bucketCount)
    {
        var min = values.Min();
        var max = values.Max();
        var bucketSize = (max - min) / (double)bucketCount;

        var buckets = new Dictionary<string, int>();

        for (int i = 0; i < bucketCount; i++)
        {
            var bucketMin = min + (i * bucketSize);
            var bucketMax = min + ((i + 1) * bucketSize);
            var label = $"{Math.Round(bucketMin)}-{Math.Round(bucketMax)}";
            var count = values.Count(v => v >= bucketMin && v < bucketMax);
            buckets[label] = count;
        }

        return buckets;
    }

    private string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "Unknown";
        }
    }

    private double ExtractConfidenceScore(string? extractedDataJson)
    {
        if (string.IsNullOrEmpty(extractedDataJson))
            return 0;

        try
        {
            using var doc = JsonDocument.Parse(extractedDataJson);
            if (doc.RootElement.TryGetProperty("confidence", out var confidenceElement))
            {
                return confidenceElement.GetDouble();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract confidence score from JSON");
        }

        return 0;
    }

    private string GetPeriodString(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
            return $"{startDate.Value:MMM dd, yyyy} - {endDate.Value:MMM dd, yyyy}";
        if (startDate.HasValue)
            return $"From {startDate.Value:MMM dd, yyyy}";
        if (endDate.HasValue)
            return $"Until {endDate.Value:MMM dd, yyyy}";
        return "All time";
    }

    private ApexChartResponse<BarChartData> CreateEmptyBarChart(string title, string subtitle)
    {
        return new ApexChartResponse<BarChartData>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = title,
                Subtitle = subtitle,
                Height = "400px"
            },
            Data = new BarChartData
            {
                Series = new List<BarSeriesItem>(),
                Categories = Array.Empty<string>()
            },
            Metadata = new ChartMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                DataPoints = 0
            }
        };
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<T>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for key {Key}", key);
        }
        return default;
    }

    private async Task SetCacheAsync<T>(string key, T value, int ttlSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };
            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for key {Key}", key);
        }
    }
}
