using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using WebCrawlerService.Application.DTOs.Charts;
using WebCrawlerService.Application.DTOs.DataVisualization;
using WebCrawlerService.Application.Services.Crawl4AI;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Services.DataVisualization;

/// <summary>
/// Service for AI-powered data visualization and chart generation from crawled data
/// </summary>
public class DataVisualizationService : IDataVisualizationService
{
    private readonly IRepository<CrawlResult> _crawlResultRepo;
    private readonly IRepository<CrawlJob> _crawlJobRepo;
    private readonly IGeminiService _geminiService;
    private readonly ICrawl4AIClientService _crawl4AIClientService;
    private readonly ILogger<DataVisualizationService> _logger;

    public DataVisualizationService(
        IRepository<CrawlResult> crawlResultRepo,
        IRepository<CrawlJob> crawlJobRepo,
        IGeminiService geminiService,
        ICrawl4AIClientService crawl4AIClientService,
        ILogger<DataVisualizationService> logger)
    {
        _crawlResultRepo = crawlResultRepo;
        _crawlJobRepo = crawlJobRepo;
        _geminiService = geminiService;
        _crawl4AIClientService = crawl4AIClientService;
        _logger = logger;
    }

    public async Task<DataAnalysisResult> AnalyzeJobDataAsync(Guid jobId, string? prompt = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing data for job {JobId} with prompt: {Prompt}", jobId, prompt ?? "<none>");

        // Verify job exists
        var job = await _crawlJobRepo.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found");
        }

        // Get all crawl results with data
        var results = await _crawlResultRepo.GetAsync(
            filter: r => r.CrawlJobId == jobId && r.ExtractedDataJson != null,
            orderBy: null,
            skip: null,
            take: null,
            cancellationToken: cancellationToken
        );

        if (!results.Any())
        {
            return new DataAnalysisResult
            {
                JobId = jobId,
                TotalUrls = 0,
                UrlsWithData = 0,
                Warnings = new List<string> { "No extracted data found for this job" }
            };
        }

        // Parse all extracted data
        var allData = new List<Dictionary<string, object>>();
        foreach (var result in results)
        {
            if (string.IsNullOrEmpty(result.ExtractedDataJson)) continue;

            try
            {
                // 🛡️ ROBUST DESERIALIZATION STRATEGY
                var json = result.ExtractedDataJson.Trim();
                List<Dictionary<string, object>>? items = null;
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (json.StartsWith("["))
                {
                    items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, options);
                }
                else if (json.StartsWith("{"))
                {
                    try 
                    {
                        var singleItem = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                        if (singleItem != null)
                        {
                            items = new List<Dictionary<string, object>> { singleItem };
                        }
                    }
                    catch (JsonException)
                    {
                        throw; 
                    }
                }

                if (items != null && items.Count > 0)
                {
                    allData.AddRange(items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse extracted data from result {ResultId}", result.Id);
            }
        }

        if (!allData.Any())
        {
            return new DataAnalysisResult
            {
                JobId = jobId,
                TotalUrls = results.Count(),
                UrlsWithData = 0,
                Warnings = new List<string> { "No parseable data found in extraction results" }
            };
        }

        // 🧹 CLEAN DATA LAYER
        // Use AI Agent to remove trash/boilerplate data before analysis
        allData = await CleanDataWithAIAsync(allData, cancellationToken);

        // Analyze schema
        var schema = AnalyzeDataSchema(allData);

        // 🧠 SMART CHART RECOMMENDATIONS (Deterministic + Heuristic)
        // Instead of relying solely on LLM (which can be slow/hallucinate), we use our smart logic first.
        var smartRecommendations = GetSmartRecommendations(allData);

        // Optional: Still get AI insights for text summary
        List<string> insights = new();
        string summaryText = string.Empty;
        try 
        {
            // We can still ask AI for text insights, but we trust our chart logic more
            var aiAnalysis = await GetAIAnalysisAsync(jobId, allData, schema, prompt, cancellationToken);
            insights = aiAnalysis.Insights ?? new List<string>();
            summaryText = aiAnalysis.SummaryText;
            
            // Merge recommendations, prioritizing AI ones (especially if they have pre-generated data)
            if (aiAnalysis.Recommendations != null && aiAnalysis.Recommendations.Any())
            {
                // Give AI recommendations higher priority so they appear first in the UI
                foreach (var r in aiAnalysis.Recommendations) 
                {
                    r.Priority = 0; 
                }
                smartRecommendations.InsertRange(0, aiAnalysis.Recommendations);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Analysis failed, falling back to smart recommendations");
            insights.Add("AI Analysis unavailable. Showing automated insights.");
        }

        return new DataAnalysisResult
        {
            JobId = jobId,
            TotalUrls = results.Count(),
            UrlsWithData = results.Count(),
            Schema = schema,
            ChartRecommendations = smartRecommendations,
            Insights = insights,
            Summary = summaryText,
            Statistics = new Dictionary<string, object> { { "RecordCount", allData.Count } },
            DataDomain = "Auto-Detected",
            AnalysisConfidence = 0.9,
            DataSample = allData.Take(5).ToList(),
            AnalyzedAt = DateTime.UtcNow
        };
    }

    public async Task<ApexChartResponse<object>> GenerateVisualizationAsync(
        Guid jobId,
        string chartType,
        VisualizationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {ChartType} visualization for job {JobId}", chartType, jobId);

        // Get analysis first
        var analysis = await AnalyzeJobDataAsync(jobId, null, cancellationToken);

        if (!analysis.DataSample.Any())
        {
            throw new InvalidOperationException("No data available for visualization");
        }

        // Get all data for visualization
        var results = await _crawlResultRepo.GetAsync(
            filter: r => r.CrawlJobId == jobId && r.ExtractedDataJson != null,
            orderBy: null,
            skip: null,
            take: null,
            cancellationToken: cancellationToken
        );

        var allData = ParseAllExtractedData(results);

        // Apply filters if specified
        if (options?.Filters != null)
        {
            allData = ApplyFilters(allData, options.Filters);
        }

        // Generate chart based on type
        return chartType.ToLower() switch
        {
            "pie" => GeneratePieChart(allData, analysis.Schema, options),
            "bar" => GenerateBarChart(allData, analysis.Schema, options),
            "line" => GenerateLineChart(allData, analysis.Schema, options),
            "scatter" => GenerateScatterChart(allData, analysis.Schema, options),
            "histogram" => GenerateHistogramChart(allData, analysis.Schema, options),
            "radial" => GenerateRadialChart(allData, analysis.Schema, options),
            "stacked-bar" => GenerateStackedBarChart(allData, analysis.Schema, options),
            _ => throw new ArgumentException($"Unsupported chart type: {chartType}")
        };
    }

    public async Task<List<ApexChartResponse<object>>> GenerateRecommendedVisualizationsAsync(
        Guid jobId,
        int maxCharts = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {MaxCharts} recommended visualizations for job {JobId}", maxCharts, jobId);

        // Get analysis with AI recommendations
        var analysis = await AnalyzeJobDataAsync(jobId, null, cancellationToken);

        if (!analysis.ChartRecommendations.Any())
        {
            throw new InvalidOperationException("No chart recommendations available");
        }

        var charts = new List<ApexChartResponse<object>>();

        // Generate top N recommended charts
        var topRecommendations = analysis.ChartRecommendations
            .OrderBy(r => r.Priority)
            .Take(maxCharts);

        foreach (var recommendation in topRecommendations)
        {
            try
            {
                var options = new VisualizationOptions
                {
                    Title = recommendation.Title,
                    XAxisField = recommendation.XAxisFields.FirstOrDefault(),
                    YAxisField = recommendation.YAxisFields.FirstOrDefault(),
                    AggregationFunction = recommendation.AggregationFunction,
                    Filters = recommendation.Filters,
                    ColorScheme = recommendation.ColorScheme
                };

                var chart = await GenerateVisualizationAsync(jobId, recommendation.ChartType, options, cancellationToken);
                charts.Add(chart);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate {ChartType} chart", recommendation.ChartType);
            }
        }

        return charts;
    }

    public async Task<DataAnalysisResult> AnalyzeUrlDataAsync(
        Guid jobId,
        string url,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing data for URL {Url} in job {JobId}", url, jobId);

        var result = await _crawlResultRepo.GetAsync(
            filter: r => r.CrawlJobId == jobId && r.Url == url,
            orderBy: null,
            skip: null,
            take: 1,
            cancellationToken: cancellationToken
        );

        var crawlResult = result.FirstOrDefault();
        if (crawlResult == null || string.IsNullOrEmpty(crawlResult.ExtractedDataJson))
        {
            throw new InvalidOperationException($"No data found for URL {url}");
        }

        var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            crawlResult.ExtractedDataJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (data == null || !data.Any())
        {
            throw new InvalidOperationException($"No parseable data found for URL {url}");
        }

        var schema = AnalyzeDataSchema(data);
        var aiAnalysis = await GetAIAnalysisAsync(jobId, data, schema, null, cancellationToken);

        return new DataAnalysisResult
        {
            JobId = jobId,
            TotalUrls = 1,
            UrlsWithData = 1,
            Schema = schema,
            ChartRecommendations = aiAnalysis.Recommendations,
            Insights = aiAnalysis.Insights,
            Statistics = aiAnalysis.Statistics,
            DataDomain = aiAnalysis.DataDomain,
            AnalysisConfidence = aiAnalysis.Confidence,
            DataSample = data.Take(5).ToList(),
            AnalyzedAt = DateTime.UtcNow
        };
    }

    public async Task<ApexChartResponse<object>> GenerateComparisonVisualizationAsync(
        Guid jobId,
        string comparisonField,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating comparison visualization for field {Field} in job {JobId}", comparisonField, jobId);

        var results = await _crawlResultRepo.GetAsync(
            filter: r => r.CrawlJobId == jobId && r.ExtractedDataJson != null,
            orderBy: null,
            skip: null,
            take: null,
            cancellationToken: cancellationToken
        );

        var allData = ParseAllExtractedData(results);

        // Group by URL and aggregate comparison field
        var comparisonData = allData
            .GroupBy(d => d.ContainsKey("url") ? d["url"] : "Unknown")
            .Select(g => new Dictionary<string, object>
            {
                ["source"] = g.Key,
                [comparisonField] = g.Average(d =>
                {
                    if (d.ContainsKey(comparisonField))
                    {
                        var value = d[comparisonField];
                        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                        {
                            return jsonElement.GetDouble();
                        }
                        if (double.TryParse(value?.ToString(), out var numValue))
                        {
                            return numValue;
                        }
                    }
                    return 0;
                })
            })
            .ToList();

        var options = new VisualizationOptions
        {
            Title = $"Comparison: {comparisonField} across sources",
            XAxisField = "source",
            YAxisField = comparisonField
        };

        var schema = AnalyzeDataSchema(comparisonData);
        return GenerateBarChart(comparisonData, schema, options);
    }

    #region Private Helper Methods

    private DataSchema AnalyzeDataSchema(List<Dictionary<string, object>> data)
    {
        if (!data.Any())
        {
            return new DataSchema { RecordCount = 0, IsStructured = false };
        }

        var schema = new DataSchema
        {
            RecordCount = data.Count,
            IsStructured = true
        };

        // Get all field names
        var allFields = data.SelectMany(d => d.Keys).Distinct().ToList();

        foreach (var fieldName in allFields)
        {
            var fieldData = data
                .Where(d => d.ContainsKey(fieldName))
                .Select(d => d[fieldName])
                .Where(v => v != null)
                .ToList();

            var coverage = (double)fieldData.Count / data.Count * 100;
            var dataType = InferDataType(fieldData);
            var semanticType = InferSemanticType(fieldName, fieldData);

            var field = new DataField
            {
                Name = fieldName,
                DataType = dataType,
                SemanticType = semanticType,
                IsRequired = coverage >= 90,
                Coverage = coverage,
                SampleValues = fieldData.Take(3).ToList(),
                IsVisualizable = IsVisualizableField(dataType, semanticType, fieldData),
                SuggestedRoles = GetSuggestedRoles(dataType, semanticType)
            };

            // Add statistics for numeric fields
            if (dataType == "number")
            {
                field.NumericStatistics = CalculateNumericStats(fieldData);
            }

            // Count unique values for categorical fields
            if (dataType == "string" && fieldData.Count <= 100)
            {
                field.UniqueValueCount = fieldData.Select(v => v?.ToString()).Distinct().Count();
            }

            schema.Fields.Add(field);
        }

        return schema;
    }

    private string InferDataType(List<object> values)
    {
        if (!values.Any()) return "unknown";

        var firstValue = values.First();

        // Handle JsonElement
        if (firstValue is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Number => "number",
                JsonValueKind.String => "string",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Array => "array",
                JsonValueKind.Object => "object",
                _ => "unknown"
            };
        }

        // Handle native types
        return firstValue switch
        {
            int or long or float or double or decimal => "number",
            string => "string",
            bool => "boolean",
            DateTime => "date",
            _ => "object"
        };
    }

    private string InferSemanticType(string fieldName, List<object> values)
    {
        var lowerName = fieldName.ToLower();

        // Common semantic patterns (English & Vietnamese)
        if (lowerName.Contains("price") || lowerName.Contains("cost") || lowerName.Contains("amount") || 
            lowerName.Contains("giá") || lowerName.Contains("tien") || lowerName.Contains("tiền"))
            return "price";
        if (lowerName.Contains("rating") || lowerName.Contains("score") || 
            lowerName.Contains("đánh giá") || lowerName.Contains("danh gia") || lowerName.Contains("sao"))
            return "rating";
        if (lowerName.Contains("percent") || lowerName.Contains("rate") || 
            lowerName.Contains("phần trăm") || lowerName.Contains("phan tram") || lowerName.Contains("tỷ lệ"))
            return "percentage";
        if (lowerName.Contains("date") || lowerName.Contains("time") || 
            lowerName.Contains("ngày") || lowerName.Contains("ngay") || lowerName.Contains("thời gian"))
            return "date";
        if (lowerName.Contains("url") || lowerName.Contains("link") || lowerName.Contains("href") || 
            lowerName.Contains("liên kết"))
            return "url";
        if (lowerName.Contains("category") || lowerName.Contains("type") || lowerName.Contains("status") || 
            lowerName.Contains("loại") || lowerName.Contains("danh mục") || lowerName.Contains("trạng thái"))
            return "category";
        if (lowerName.Contains("count") || lowerName.Contains("quantity") || lowerName.Contains("number") || 
            lowerName.Contains("số lượng") || lowerName.Contains("so luong") || lowerName.Contains("tổng"))
            return "quantity";
        if (lowerName.Contains("name") || lowerName.Contains("title") || 
            lowerName.Contains("tên") || lowerName.Contains("tiêu đề"))
            return "label";

        return "general";
    }

    private bool IsVisualizableField(string dataType, string semanticType, List<object> values)
    {
        // URLs and objects are not directly visualizable
        if (semanticType == "url" || dataType == "object") return false;

        // Very large text fields are not good for visualization
        if (dataType == "string")
        {
            var avgLength = values
                .Select(v => v?.ToString()?.Length ?? 0)
                .DefaultIfEmpty(0)
                .Average();

            if (avgLength > 100) return false; // Long descriptions
        }

        return true;
    }

    private List<string> GetSuggestedRoles(string dataType, string semanticType)
    {
        var roles = new List<string>();

        if (semanticType == "category" || semanticType == "label")
        {
            roles.Add("x-axis");
            roles.Add("category");
        }

        if (dataType == "number" || semanticType == "price" || semanticType == "rating" || semanticType == "quantity")
        {
            roles.Add("y-axis");
            roles.Add("value");
        }

        if (semanticType == "date")
        {
            roles.Add("x-axis");
            roles.Add("timeline");
        }

        return roles;
    }

    private NumericStats CalculateNumericStats(List<object> values)
    {
        var numericValues = values
            .Select(v =>
            {
                if (v is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetDouble();
                if (double.TryParse(v?.ToString(), out var num))
                    return num;
                return (double?)null;
            })
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (!numericValues.Any())
        {
            return new NumericStats();
        }

        var sorted = numericValues.OrderBy(v => v).ToList();
        var mean = numericValues.Average();
        var variance = numericValues.Select(v => Math.Pow(v - mean, 2)).Average();

        return new NumericStats
        {
            Min = sorted.First(),
            Max = sorted.Last(),
            Mean = mean,
            Median = sorted.Count % 2 == 0
                ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
                : sorted[sorted.Count / 2],
            StandardDeviation = Math.Sqrt(variance),
            Count = numericValues.Count
        };
    }

    private async Task<(List<ChartRecommendation> Recommendations, List<string> Insights, Dictionary<string, object> Statistics, string DataDomain, double Confidence, string SummaryText)>
        GetAIAnalysisAsync(Guid jobId, List<Dictionary<string, object>> data, DataSchema schema, string? prompt, CancellationToken cancellationToken)
    {
        try
        {
            // Use Python Agent for summary
            var summary = await _crawl4AIClientService.GenerateSummaryAsync(jobId.ToString(), data, prompt, cancellationToken);
            
            var recommendations = new List<ChartRecommendation>();
            
            // Map ChartPreviews to ChartRecommendation (Best Effort)
            foreach (var chart in summary.Charts)
            {
                recommendations.Add(new ChartRecommendation
                {
                    ChartType = chart.ChartType,
                    Title = chart.Title,
                    Reasoning = chart.Reasoning,
                    Priority = 1,
                    Confidence = chart.Confidence,
                    ExpectedInsights = chart.ExpectedInsights,
                    XAxisFields = chart.XAxisFields,
                    YAxisFields = chart.YAxisFields,
                    AggregationFunction = chart.AggregationFunction,
                    PreGeneratedData = chart.PreGeneratedData
                });
            }

            return (
                recommendations,
                summary.InsightHighlights,
                new Dictionary<string, object>(), // Statistics
                "Auto-Detected", // DataDomain
                0.9, // Confidence
                summary.SummaryText
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI analysis failed, falling back to basic recommendations");
            return (
                new List<ChartRecommendation>(),
                new List<string> { "AI Analysis unavailable." },
                new Dictionary<string, object>(),
                "Unknown",
                0.0,
                string.Empty
            );
        }
    }

    private (List<ChartRecommendation> Recommendations, List<string> Insights, Dictionary<string, object> Statistics, string DataDomain, double Confidence)
        GenerateFallbackRecommendations(DataSchema schema)
    {
        var recommendations = new List<ChartRecommendation>();
        var insights = new List<string> { "Data contains " + schema.RecordCount + " records" };
        var statistics = new Dictionary<string, object> { ["totalRecords"] = schema.RecordCount };

        // Find categorical and numeric fields
        var categoricalFields = schema.Fields.Where(f => f.SemanticType == "category" || f.SemanticType == "label").ToList();
        var numericFields = schema.Fields.Where(f => f.DataType == "number").ToList();

        // Recommend pie chart for categorical distribution
        if (categoricalFields.Any())
        {
            recommendations.Add(new ChartRecommendation
            {
                ChartType = "pie",
                Title = $"Distribution by {categoricalFields.First().Name}",
                Reasoning = "Shows the distribution of records across categories",
                XAxisFields = new List<string> { categoricalFields.First().Name },
                YAxisFields = new List<string>(),
                AggregationFunction = "count",
                Confidence = 0.7,
                Priority = 1
            });
        }

        // Recommend bar chart for numeric comparison
        if (categoricalFields.Any() && numericFields.Any())
        {
            recommendations.Add(new ChartRecommendation
            {
                ChartType = "bar",
                Title = $"{numericFields.First().Name} by {categoricalFields.First().Name}",
                Reasoning = "Compares numeric values across categories",
                XAxisFields = new List<string> { categoricalFields.First().Name },
                YAxisFields = new List<string> { numericFields.First().Name },
                AggregationFunction = "avg",
                Confidence = 0.7,
                Priority = 2
            });
        }

        return (recommendations, insights, statistics, "General Data", 0.5);
    }

    private List<Dictionary<string, object>> ParseAllExtractedData(IEnumerable<CrawlResult> results)
    {
        var allData = new List<Dictionary<string, object>>();

        foreach (var result in results)
        {
            if (string.IsNullOrEmpty(result.ExtractedDataJson)) continue;

            try
            {
                // 🔍 FIX: Handle both Array [...] and Single Object {...} JSON formats
                List<Dictionary<string, object>>? data = null;
                var json = result.ExtractedDataJson.Trim();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (json.StartsWith("["))
                {
                    // Standard case: List of items
                    data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, options);
                }
                else 
                {
                    // Edge case: Single item returned as object (common for product detail pages)
                    try 
                    {
                        var singleItem = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                        if (singleItem != null)
                        {
                            data = new List<Dictionary<string, object>> { singleItem };
                        }
                    }
                    catch (JsonException)
                    {
                        // If it's neither a valid list nor a valid object, rethrow to be caught below
                        throw;
                    }
                }

                if (data != null)
                {
                    // Add source URL to each record
                    foreach (var record in data)
                    {
                        if (!record.ContainsKey("_sourceUrl"))
                        {
                            record["_sourceUrl"] = result.Url;
                        }
                    }

                    allData.AddRange(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse data from result {ResultId}", result.Id);
            }
        }

        return allData;
    }

    private List<Dictionary<string, object>> ApplyFilters(List<Dictionary<string, object>> data, Dictionary<string, object> filters)
    {
        foreach (var filter in filters)
        {
            data = data.Where(d =>
            {
                if (!d.ContainsKey(filter.Key)) return false;

                var value = d[filter.Key];
                var filterValue = filter.Value;

                // Handle JsonElement comparison
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.ToString() == filterValue?.ToString();
                }

                return value?.ToString() == filterValue?.ToString();
            }).ToList();
        }

        return data;
    }

    // Chart generation methods (implementations in next part)
    private ApexChartResponse<object> GeneratePieChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var categoryField = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.SemanticType == "category")?.Name ?? schema.Fields.First().Name;

        var grouped = data
            .GroupBy(d => d.ContainsKey(categoryField) ? d[categoryField]?.ToString() ?? "Unknown" : "Unknown")
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (options?.TopN.HasValue == true)
        {
            grouped = grouped.Take(options.TopN.Value).ToList();
        }

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration
            {
                Type = "pie",
                Title = options?.Title ?? $"Distribution by {categoryField}"
            },
            Colors = ResolveColors(options, grouped.Count),
            Data = new PieChartData
            {
                Labels = grouped.Select(x => x.Category).ToArray(),
                Series = grouped.Select(x => (double)x.Count).ToArray()
            }
        };
    }

    private ApexChartResponse<object> GenerateBarChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var xField = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.SemanticType == "category")?.Name ?? schema.Fields.First().Name;
        var yField = options?.YAxisField ?? schema.Fields.FirstOrDefault(f => f.DataType == "number")?.Name;
        var aggregation = options?.AggregationFunction?.ToLower() ?? "avg";

        if (yField == null && aggregation != "count")
        {
            throw new InvalidOperationException("No numeric field available for Y-axis");
        }

        var grouped = data
            .GroupBy(d => d.ContainsKey(xField) ? d[xField]?.ToString() ?? "Unknown" : "Unknown")
            .Select(g => new
            {
                Category = g.Key,
                Value = aggregation switch
                {
                    "sum" => g.Sum(d => GetNumericValue(d, yField!)),
                    "avg" or "average" => g.Average(d => GetNumericValue(d, yField!)),
                    "min" => g.Min(d => GetNumericValue(d, yField!)),
                    "max" => g.Max(d => GetNumericValue(d, yField!)),
                    "count" => g.Count(),
                    _ => g.Average(d => GetNumericValue(d, yField!)) // default to average
                }
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        if (options?.TopN.HasValue == true)
        {
            grouped = grouped.Take(options.TopN.Value).ToList();
        }

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = options?.Title ?? $"{yField} by {xField}"
            },
            Colors = ResolveColors(options, grouped.Count),
            Data = new BarChartData
            {
                Categories = grouped.Select(x => x.Category).ToArray(),
                Series = new List<BarSeriesItem>
                {
                    new BarSeriesItem
                    {
                        Name = yField,
                        Data = grouped.Select(x => x.Value).ToArray()
                    }
                }
            }
        };
    }

    private ApexChartResponse<object> GenerateLineChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        // Similar to bar chart but with line type
        var barChart = GenerateBarChart(data, schema, options);
        barChart.Chart.Type = "line";
        return barChart;
    }

    private ApexChartResponse<object> GenerateScatterChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var xField = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.DataType == "number")?.Name;
        var yField = options?.YAxisField ?? schema.Fields.Where(f => f.DataType == "number" && f.Name != xField).FirstOrDefault()?.Name;

        if (xField == null || yField == null)
        {
            throw new InvalidOperationException("Need two numeric fields for scatter plot");
        }

        var points = data
            .Select(d => new[] { GetNumericValue(d, xField), GetNumericValue(d, yField) })
            .ToList();

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration
            {
                Type = "scatter",
                Title = options?.Title ?? $"{yField} vs {xField}"
            },
            Colors = ResolveColors(options, 1),
            Data = new
            {
                Series = new[]
                {
                    new
                    {
                        Name = "Data Points",
                        Data = points
                    }
                }
            }
        };
    }

    private ApexChartResponse<object> GenerateHistogramChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var field = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.DataType == "number")?.Name;

        if (field == null)
        {
            throw new InvalidOperationException("Need numeric field for histogram");
        }

        return GenerateHistogram(data, field, options?.Title ?? $"Distribution of {field}");
    }

    private ApexChartResponse<object> GenerateRadialChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var field = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.SemanticType == "percentage" || f.SemanticType == "rating")?.Name;

        if (field == null)
        {
            throw new InvalidOperationException("Need percentage or rating field for radial chart");
        }

        var avgValue = data.Average(d => GetNumericValue(d, field));

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration
            {
                Type = "radialBar",
                Title = options?.Title ?? $"Average {field}"
            },
            Colors = ResolveColors(options, 1),
            Data = new RadialBarData
            {
                Series = new[] { avgValue },
                Labels = new[] { field }
            }
        };
    }

    private ApexChartResponse<object> GenerateStackedBarChart(List<Dictionary<string, object>> data, DataSchema schema, VisualizationOptions? options)
    {
        var xField = options?.XAxisField ?? schema.Fields.FirstOrDefault(f => f.SemanticType == "category")?.Name;
        var yField = options?.YAxisField ?? schema.Fields.FirstOrDefault(f => f.DataType == "number")?.Name;
        var groupField = options?.GroupByField ?? schema.Fields.Where(f => f.SemanticType == "category" && f.Name != xField).FirstOrDefault()?.Name;

        if (xField == null || yField == null || groupField == null)
        {
            throw new InvalidOperationException("Need category, numeric, and group fields for stacked bar chart");
        }

        var groups = data.Select(d => d.ContainsKey(groupField) ? d[groupField]?.ToString() ?? "Unknown" : "Unknown").Distinct().ToList();
        var categories = data.Select(d => d.ContainsKey(xField) ? d[xField]?.ToString() ?? "Unknown" : "Unknown").Distinct().ToList();

        var series = groups.Select(group => new BarSeriesItem
        {
            Name = group,
            Data = categories.Select(cat =>
            {
                var items = data.Where(d =>
                    (d.ContainsKey(xField) ? d[xField]?.ToString() : "Unknown") == cat &&
                    (d.ContainsKey(groupField) ? d[groupField]?.ToString() : "Unknown") == group
                ).ToList();

                return items.Any() ? items.Average(d => GetNumericValue(d, yField)) : 0;
            }).ToArray()
        }).ToList();

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration
            {
                Type = "bar",
                Title = options?.Title ?? $"{yField} by {xField} (grouped by {groupField})",
                Stacked = true
            },
            Colors = ResolveColors(options, series.Count),
            Data = new BarChartData
            {
                Categories = categories.ToArray(),
                Series = series
            }
        };
    }

    private double GetNumericValue(Dictionary<string, object> record, string fieldName)
    {
        if (!record.ContainsKey(fieldName)) return 0;

        var value = record[fieldName];

        // Handle JsonElement values from System.Text.Json deserialization
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                return jsonElement.GetDouble();
            }
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var strVal = jsonElement.GetString();
                var parsed = TryParseFlexibleNumber(strVal);
                if (parsed.HasValue) return parsed.Value;
            }
        }

        // Handle plain strings
        if (value is string strValue)
        {
            var parsed = TryParseFlexibleNumber(strValue);
            if (parsed.HasValue) return parsed.Value;
        }

        // Fallback: attempt raw parse
        if (double.TryParse(value?.ToString(), out var numValue))
        {
            return numValue;
        }

        return 0;
    }

    private double? TryParseFlexibleNumber(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // Remove currency symbols and spaces (including non-breaking spaces)
        var normalized = input
            .Replace("\u00A0", "") // NBSP
            .Replace("\u2009", "") // thin space
            .Replace("\u202F", ""); // narrow NBSP

        // Keep digits, comma, dot, minus
        normalized = Regex.Replace(normalized, @"[^\d,\.\-]", "");
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        // If both comma and dot exist, assume they are thousand separators -> strip both
        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.Replace(",", "").Replace(".", "");
        }
        else if (normalized.Contains(','))
        {
            // If comma used as thousand separator (most prices), strip it
            normalized = normalized.Replace(",", "");
        }
        else if (normalized.Contains('.'))
        {
            // If dot used as thousand separator, strip it
            normalized = normalized.Replace(".", "");
        }

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private string[] ResolveColors(VisualizationOptions? options, int count)
    {
        if (options?.CustomColors != null && options.CustomColors.Any())
        {
            return options.CustomColors.ToArray();
        }

        return GetColorPalette(options?.ColorScheme, count);
    }

    private string[] GetColorPalette(string? scheme, int count)
    {
        var palettes = new Dictionary<string, string[]>
        {
            ["default"] = new[] { "#008FFB", "#00E396", "#FEB019", "#FF4560", "#775DD0", "#546E7A", "#26a69a", "#D10CE8" },
            ["vibrant"] = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF", "#FF9F40", "#FF6384", "#C9CBCF" },
            ["pastel"] = new[] { "#FFB6C1", "#87CEEB", "#98FB98", "#DDA0DD", "#F0E68C", "#FFA07A", "#E0BBE4", "#FFDAB9" },
            ["monochrome"] = new[] { "#1a1a1a", "#333333", "#4d4d4d", "#666666", "#808080", "#999999", "#b3b3b3", "#cccccc" }
        };

        var palette = palettes.ContainsKey(scheme ?? "default") ? palettes[scheme ?? "default"] : palettes["default"];

        // Repeat palette if more colors needed
        return Enumerable.Range(0, count)
            .Select(i => palette[i % palette.Length])
            .ToArray();
    }

    private async Task<List<Dictionary<string, object>>> CleanDataWithAIAsync(List<Dictionary<string, object>> data, CancellationToken cancellationToken)
    {
        if (data.Count == 0) return data;

        try 
        {
            // 1. Basic Heuristic Cleaning first (to reduce token usage)
            // Remove fields that are 100% null or empty strings
            var allKeys = data.SelectMany(d => d.Keys).Distinct().ToList();
            var keysToRemove = new List<string>();

            foreach (var key in allKeys)
            {
                var values = data.Select(d => d.ContainsKey(key) ? d[key]?.ToString() : null).ToList();
                
                // Rule 1: 100% Empty/Null
                if (values.All(string.IsNullOrWhiteSpace))
                {
                    keysToRemove.Add(key);
                    continue;
                }

                // Rule 2: Constant Value (Boilerplate)
                // We remove it if it's long (likely legal text/nav) or looks like internal ID
                var distinctValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
                if (distinctValues.Count == 1 && data.Count > 5)
                {
                    var val = distinctValues[0];
                    // Remove if it's a long string (boilerplate) or a URL (nav link)
                    if (val.Length > 30 || (val.StartsWith("http") && key.ToLower().Contains("nav"))) 
                    {
                        keysToRemove.Add(key);
                    }
                }
            }

            // Remove heuristic keys
            if (keysToRemove.Any())
            {
                foreach (var record in data)
                {
                    foreach (var key in keysToRemove)
                    {
                        record.Remove(key);
                    }
                }
                _logger.LogInformation("Removed {Count} empty/boilerplate fields via heuristics: {Fields}", keysToRemove.Count, string.Join(", ", keysToRemove));
            }

            // 2. Type Inference & Fixing (Currency Cleaning)
            foreach (var row in data)
            {
                var keys = row.Keys.ToList();
                foreach (var key in keys)
                {
                    var val = row[key];
                    if (val is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        s = s.Trim();
                        // Currency cleaning
                        if ((s.StartsWith("$") || s.StartsWith("€") || s.StartsWith("£") || s.EndsWith("đ") || s.EndsWith("vnd")) 
                            && s.Length < 20)
                        {
                            var cleanStr = s.Replace("$", "").Replace("€", "").Replace("£", "").Replace("đ", "").Replace("vnd", "").Replace(",", "").Trim();
                            if (decimal.TryParse(cleanStr, out var d))
                            {
                                row[key] = d;
                            }
                        }
                        // Handle pure numbers in strings (e.g. "12.99")
                        else if (decimal.TryParse(s, out var d))
                        {
                            row[key] = d;
                        }
                    }
                }
            }

            // 3. AI Cleaning (Disabled due to Gemini 404s - relying on heuristics)
            /*
            // Take a sample to send to LLM
            var sample = data.Take(5).ToList();
            var sampleJson = JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $@"You are a Data Cleaning Agent. Your task is to identify ""trash"" or ""irrelevant"" fields in the provided dataset that should be removed before visualization.

TRASH DATA DEFINITION:
- Fields containing boilerplate text (e.g., ""Copyright 2023"", ""All rights reserved"", ""Menu"", ""Skip to content"").
- Fields containing only URLs to assets like icons, spacers, or tracking pixels (unless it's a main product image).
- Fields containing internal IDs or hashes that are not useful for business analysis (e.g., ""__typename"", ""data-reactid"").
- Fields that are navigation links (e.g., ""Home"", ""About Us"", ""Contact"").
- Fields that contain raw HTML or scripts.

DATA SAMPLE (first 5 records):
{sampleJson}

TASK:
Identify the field names that match the ""Trash Data"" definition.

Return ONLY a valid JSON array of strings (field names to remove). Example: [""copyright_text"", ""nav_link"", ""spacer_gif""]
If no fields should be removed, return [].";

            var response = await _geminiService.GenerateContentAsync(prompt, cancellationToken);
            
            // Parse response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var aiKeysToRemove = JsonSerializer.Deserialize<List<string>>(jsonText);

                if (aiKeysToRemove != null && aiKeysToRemove.Any())
                {
                    int removedCount = 0;
                    foreach (var record in data)
                    {
                        foreach (var key in aiKeysToRemove)
                        {
                            if (record.ContainsKey(key))
                            {
                                record.Remove(key);
                                removedCount++;
                            }
                        }
                    }
                    _logger.LogInformation("Removed {Count} fields via AI cleaning: {Fields}", removedCount, string.Join(", ", aiKeysToRemove));
                }
            }
            */
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Data cleaning failed");
            return data;
        }
    }

    private List<ChartRecommendation> GetSmartRecommendations(List<Dictionary<string, object>> data)
    {
        var recommendations = new List<ChartRecommendation>();
        if (data.Count == 0) return recommendations;

        var keys = data.First().Keys.ToList();
        // Re-evaluate numeric fields after cleaning
        var numericFields = keys.Where(k => data.Any(d => d[k] is decimal || d[k] is double || d[k] is int || d[k] is long)).ToList();
        var stringFields = keys.Except(numericFields).ToList();

        // 1. Price Histogram
        var priceField = numericFields.FirstOrDefault(k => k.ToLower().Contains("price") || k.ToLower().Contains("cost") || k.ToLower().Contains("amount"));
        if (priceField != null)
        {
            recommendations.Add(new ChartRecommendation
            {
                ChartType = "histogram",
                Title = "Price Distribution",
                Reasoning = "Distribution of prices helps identify common price points and outliers.",
                XAxisFields = new List<string> { priceField },
                Priority = 1
            });
        }
        // Fallback: If no explicit price field, try ANY numeric field with variance
        else if (numericFields.Any())
        {
             var bestNumeric = numericFields.OrderByDescending(f => data.Select(d => d[f]?.ToString()).Distinct().Count()).First();
             recommendations.Add(new ChartRecommendation
            {
                ChartType = "histogram",
                Title = $"{bestNumeric} Distribution",
                Reasoning = $"Distribution of {bestNumeric}",
                XAxisFields = new List<string> { bestNumeric },
                Priority = 2
            });
        }

        // 2. Categorical Charts
        foreach (var field in stringFields)
        {
            if (field.ToLower().Contains("url") || field.ToLower().Contains("image") || field.ToLower().Contains("link")) continue;

            var distinctCount = data.Select(d => d.ContainsKey(field) ? d[field]?.ToString() : null).Distinct().Count();
            
            if (distinctCount > 1 && distinctCount < 15)
            {
                recommendations.Add(new ChartRecommendation
                {
                    ChartType = "pie",
                    Title = $"{field} Distribution",
                    Reasoning = $"Low cardinality field ({distinctCount} unique values) is perfect for pie charts.",
                    XAxisFields = new List<string> { field },
                    Priority = 2
                });
            }
            else if (distinctCount >= 15 && distinctCount < data.Count * 0.95)
            {
                recommendations.Add(new ChartRecommendation
                {
                    ChartType = "bar",
                    Title = $"Top 10 {field}",
                    Reasoning = $"High cardinality field ({distinctCount} values), showing top 10.",
                    XAxisFields = new List<string> { field },
                    AggregationFunction = "count",
                    Priority = 3
                });
            }
        }

        return recommendations.OrderBy(r => r.Priority).ToList();
    }

    private ApexChartResponse<object> GenerateHistogram(List<Dictionary<string, object>> data, string field, string title)
    {
        var values = data.Select(d => GetNumericValue(d, field)).OrderBy(v => v).ToList();
        if (!values.Any()) return new ApexChartResponse<object>();

        // Simple binning (Sturges' rule)
        int binCount = (int)Math.Ceiling(Math.Log2(values.Count) + 1);
        double min = values.First();
        double max = values.Last();
        double width = (max - min) / binCount;
        if (width == 0) width = 1; // Handle single value case

        var bins = new int[binCount];
        var categories = new string[binCount];

        for (int i = 0; i < binCount; i++)
        {
            double binStart = min + (i * width);
            double binEnd = min + ((i + 1) * width);
            categories[i] = $"{binStart:N0}-{binEnd:N0}";
        }

        foreach (var v in values)
        {
            int binIndex = (int)((v - min) / width);
            if (binIndex >= binCount) binIndex = binCount - 1;
            if (binIndex < 0) binIndex = 0;
            bins[binIndex]++;
        }

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration { Type = "bar", Title = title },
            Colors = ResolveColors(null, categories.Length),
            Data = new BarChartData
            {
                Categories = categories,
                Series = new List<BarSeriesItem> 
                { 
                    new BarSeriesItem 
                    { 
                        Name = "Count", 
                        Data = bins.Select(b => (double)b).ToArray() 
                    } 
                }
            }
        };
    }

    private ApexChartResponse<object> GenerateTopItemsChart(List<Dictionary<string, object>> data, string field, int topN)
    {
        var groups = data.GroupBy(d => d.ContainsKey(field) ? d[field]?.ToString() ?? "Unknown" : "Unknown")
                         .Select(g => new { Name = g.Key, Count = g.Count() })
                         .OrderByDescending(x => x.Count)
                         .Take(topN)
                         .ToList();

        return new ApexChartResponse<object>
        {
            Chart = new ChartConfiguration { Type = "bar", Title = $"Top {topN} {field}" },
            Data = new BarChartData
            {
                Categories = groups.Select(g => g.Name).ToArray(),
                Series = new List<BarSeriesItem> 
                { 
                    new BarSeriesItem 
                    { 
                        Name = "Count", 
                        Data = groups.Select(g => (double)g.Count).ToArray() 
                    } 
                }
            }
        };
    }

    #endregion

    #region Helper Classes

    private class AIAnalysisResponse
    {
        public string? DataDomain { get; set; }
        public double Confidence { get; set; }
        public List<string>? Insights { get; set; }
        public Dictionary<string, object>? Statistics { get; set; }
        public List<ChartRecommendation>? ChartRecommendations { get; set; }
    }

    #endregion
}
