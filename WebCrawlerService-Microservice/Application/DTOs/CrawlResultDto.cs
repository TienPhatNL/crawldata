using System.Text.Json;

namespace WebCrawlerService.Application.DTOs;

/// <summary>
/// DTO for individual crawl result with extracted data
/// </summary>
public class CrawlResultDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public Dictionary<string, object>? ExtractedData { get; set; }
    public string? Title { get; set; }
    public int HttpStatusCode { get; set; }
    public DateTime CrawledAt { get; set; }
    public int ResponseTimeMs { get; set; }
    public double ExtractionConfidence { get; set; }
    public string? ErrorMessage { get; set; }
    public long ContentSize { get; set; }
    public string? PromptUsed { get; set; }

    /// <summary>
    /// Parse ExtractedDataJson string to Dictionary
    /// </summary>
    public static Dictionary<string, object>? ParseExtractedData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
