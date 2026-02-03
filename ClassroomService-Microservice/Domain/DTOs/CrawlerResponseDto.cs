namespace ClassroomService.Domain.DTOs;

public class CrawlJobResponse
{
    public Guid JobId { get; set; }
    public int Status { get; set; } // JobStatus enum from WebCrawlerService
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? EstimatedCompletionSeconds { get; set; }
}

public class CrawlJobStatusResponse
{
    public Guid JobId { get; set; }
    public int Status { get; set; } // JobStatus enum from WebCrawlerService
    public int Progress { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public CrawlResultSummaryDto? Summary { get; set; }
}

public class CrawlResultSummaryDto
{
    public Guid ResultId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ExtractedDataJson { get; set; }
    public string? ScreenshotUrl { get; set; }
    public DateTime CrawledAt { get; set; }
}

public class CrawlResultDetailDto
{
    public Guid ResultId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ExtractedDataJson { get; set; }
    public string? ScreenshotBase64 { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public decimal? AiExtractionCost { get; set; }
    public DateTime CrawledAt { get; set; }
    
    // OPTIMIZED: Pre-generated embedding data from Python agent (eliminates separate API call)
    public string? EmbeddingText { get; set; }
    public List<double>? EmbeddingVector { get; set; }
    public string? SchemaType { get; set; } // "product_list", "article", "generic_data"
    public double? DataQualityScore { get; set; } // 0.0 - 1.0
}
