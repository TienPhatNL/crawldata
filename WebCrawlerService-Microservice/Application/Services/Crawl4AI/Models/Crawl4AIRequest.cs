using System.Text.Json.Serialization;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services.Crawl4AI.Models;

/// <summary>
/// Request for crawl4ai intelligent crawl
/// </summary>
public class Crawl4AIRequest
{
	/// <summary>
	/// Target URL to crawl
	/// </summary>
	[JsonPropertyName("url")]
	public string Url { get; set; } = null!;

	/// <summary>
	/// Natural language prompt describing what to extract
	/// </summary>
	[JsonPropertyName("prompt")]
	public string Prompt { get; set; } = null!;

	/// <summary>
	/// Optional job ID for Kafka progress tracking
	/// </summary>
	[JsonPropertyName("job_id")]
	public string? JobId { get; set; }

	/// <summary>
	/// Optional user ID for Kafka progress tracking
	/// </summary>
	[JsonPropertyName("user_id")]
	public string? UserId { get; set; }

	/// <summary>
	/// Optional pre-defined navigation steps
	/// If null, crawl4ai will analyze page and generate steps
	/// </summary>
	[JsonPropertyName("navigation_steps")]
	public List<Dictionary<string, object>>? NavigationSteps { get; set; }

	/// <summary>
	/// Optional extraction schema
	/// </summary>
	[JsonPropertyName("extract_schema")]
	public Dictionary<string, object>? ExtractSchema { get; set; }

	/// <summary>
	/// Maximum pages to collect during pagination.
	/// Null = UI field was empty (Python will try prompt extraction, fallback to 50)
	/// </summary>
	[JsonPropertyName("max_pages")]
	public int? MaxPages { get; set; }
}
