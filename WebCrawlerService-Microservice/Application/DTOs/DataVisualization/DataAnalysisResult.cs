namespace WebCrawlerService.Application.DTOs.DataVisualization;

/// <summary>
/// Result of AI-powered analysis of crawled data
/// </summary>
public class DataAnalysisResult
{
    /// <summary>
    /// The crawl job ID that was analyzed
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Total number of URLs analyzed
    /// </summary>
    public int TotalUrls { get; set; }

    /// <summary>
    /// Number of URLs with extractable data
    /// </summary>
    public int UrlsWithData { get; set; }

    /// <summary>
    /// Detected schema structure of the extracted data
    /// </summary>
    public DataSchema Schema { get; set; } = new();

    /// <summary>
    /// AI-generated summary text
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// AI-generated recommendations for chart types
    /// </summary>
    public List<ChartRecommendation> ChartRecommendations { get; set; } = new();

    /// <summary>
    /// AI-generated insights about the data
    /// </summary>
    public List<string> Insights { get; set; } = new();

    /// <summary>
    /// Summary statistics about the data
    /// </summary>
    public Dictionary<string, object> Statistics { get; set; } = new();

    /// <summary>
    /// Detected data type/domain (e.g., "E-commerce Products", "Job Listings", "News Articles")
    /// </summary>
    public string DataDomain { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the analysis (0-1)
    /// </summary>
    public double AnalysisConfidence { get; set; }

    /// <summary>
    /// When the analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sample of the actual extracted data (first few records)
    /// </summary>
    public List<Dictionary<string, object>> DataSample { get; set; } = new();

    /// <summary>
    /// Warning or error messages from the analysis
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
