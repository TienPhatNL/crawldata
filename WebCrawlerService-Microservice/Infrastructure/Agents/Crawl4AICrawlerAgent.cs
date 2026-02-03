using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models.Crawl4AI;

namespace WebCrawlerService.Infrastructure.Agents;

/// <summary>
/// Crawler agent for Crawl4AI intelligent crawling
/// Leverages Gemini 2.0 Flash for prompt analysis, navigation planning, and intelligent data extraction
/// </summary>
public class Crawl4AICrawlerAgent : ICrawlerAgent
{
    private readonly ICrawl4AIClientService _crawl4AIClient;
    private readonly ILogger<Crawl4AICrawlerAgent> _logger;

    public Crawl4AICrawlerAgent(
        ICrawl4AIClientService crawl4AIClient,
        ILogger<Crawl4AICrawlerAgent> logger)
    {
        _crawl4AIClient = crawl4AIClient;
        _logger = logger;
    }

    /// <summary>
    /// Check if this agent can handle the given crawl job
    /// </summary>
    public Task<bool> CanHandleAsync(CrawlJob job, CancellationToken cancellationToken = default)
    {
        // Handle jobs explicitly requesting Crawl4AI
        if (job.CrawlerType == CrawlerType.Crawl4AI)
        {
            return Task.FromResult(true);
        }

        // Handle jobs with natural language prompts
        if (!string.IsNullOrWhiteSpace(job.UserPrompt))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Execute intelligent crawl using Crawl4AI
    /// </summary>
    public async Task<List<CrawlResult>> ExecuteAsync(
        CrawlJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Crawl4AICrawlerAgent executing job {JobId} with {UrlCount} URLs",
            job.Id, job.Urls.Length);

        var results = new List<CrawlResult>();
        var startTime = DateTime.UtcNow;

        // Validate prompt is provided
        if (string.IsNullOrWhiteSpace(job.UserPrompt))
        {
            _logger.LogWarning("Job {JobId} has no user prompt for intelligent crawling", job.Id);
            return job.Urls.Select(url => CreateFailedResult(job.Id, url,
                "User prompt is required for Crawl4AI intelligent crawling")).ToList();
        }

        foreach (var url in job.Urls)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Crawl job {JobId} cancelled", job.Id);
                break;
            }

            try
            {
                var result = await CrawlSingleUrlAsync(job, url, cancellationToken);
                results.Add(result);

                _logger.LogDebug("Successfully crawled {Url} for job {JobId}", url, job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to crawl {Url} for job {JobId}", url, job.Id);
                results.Add(CreateFailedResult(job.Id, url, ex.Message));
            }
        }

        var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation(
            "Crawl4AICrawlerAgent completed job {JobId}: {SuccessCount}/{TotalCount} successful in {TimeMs}ms",
            job.Id, results.Count(r => r.IsSuccess), results.Count, totalTime);

        return results;
    }

    /// <summary>
    /// Crawl a single URL using Crawl4AI intelligent crawling
    /// </summary>
    private async Task<CrawlResult> CrawlSingleUrlAsync(
        CrawlJob job,
        string url,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting Crawl4AI intelligent crawl for {Url} with prompt: {Prompt}",
                url, job.UserPrompt);

            // Execute intelligent crawl with prompt
            var crawl4AIResponse = await _crawl4AIClient.IntelligentCrawlAsync(
                url,
                job.UserPrompt!,
                navigationSteps: null, // Let Crawl4AI determine navigation automatically
                jobId: job.Id.ToString(),
                userId: job.UserId.ToString(),
                cancellationToken
            );

            // Check if crawl succeeded
            if (!crawl4AIResponse.Success)
            {
                return CreateFailedResult(
                    job.Id,
                    url,
                    crawl4AIResponse.Error ?? "Crawl4AI returned unsuccessful response");
            }

            // Convert Crawl4AI response to CrawlResult
            var crawlResult = ConvertToCrawlResult(job.Id, url, crawl4AIResponse);
            crawlResult.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Crawl4AI extracted {ItemCount} items from {Url} in {TimeMs}ms",
                crawl4AIResponse.Data.Count, url, crawl4AIResponse.ExecutionTimeMs);

            return crawlResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Crawl4AI crawl for {Url}: {Message}", url, ex.Message);
            return CreateFailedResult(job.Id, url, $"HTTP error: {ex.Message}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("deserialize"))
        {
            _logger.LogError(ex, "Deserialization error for {Url}: {Message}", url, ex.Message);
            return CreateFailedResult(job.Id, url, $"Response parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error crawling {Url}", url);
            return CreateFailedResult(job.Id, url, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert Crawl4AI response to CrawlResult entity
    /// </summary>
    private CrawlResult ConvertToCrawlResult(Guid jobId, string url, Crawl4AIResponse response)
    {
        // Prepare extracted data with metadata
        var extractedData = new
        {
            items = response.Data,
            itemCount = response.Data.Count,
            executionTimeMs = response.ExecutionTimeMs,
            navigation = response.NavigationResult != null ? new
            {
                finalUrl = response.NavigationResult.FinalUrl,
                stepsExecuted = response.NavigationResult.ExecutedSteps.Count,
                pagesCollected = response.NavigationResult.PagesCollected,
                steps = response.NavigationResult.ExecutedSteps
            } : null,
            crawledAt = DateTime.UtcNow
        };

        var extractedDataJson = JsonSerializer.Serialize(extractedData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Extract title and description from first item if available
        string? title = null;
        string? description = null;
        List<string> images = new();

        if (response.Data.Count > 0)
        {
            var firstItem = response.Data[0];

            // Try common field names for title
            title = TryGetStringValue(firstItem, "title", "name", "heading", "product_name");

            // Try common field names for description
            description = TryGetStringValue(firstItem, "description", "summary", "content", "text");

            // Try to extract images
            images = TryGetImageList(firstItem);
        }

        return new CrawlResult
        {
            Id = Guid.NewGuid(),
            CrawlJobId = jobId,
            Url = url,
            HttpStatusCode = 200, // Crawl4AI doesn't expose HTTP status
            ContentType = "application/json",
            IsSuccess = true,
            CrawledAt = DateTime.UtcNow,
            Title = title ?? url,
            Description = description?.Length > 1000
                ? description[..1000] + "..."
                : description ?? $"Extracted {response.Data.Count} items",
            ExtractedDataJson = extractedDataJson,
            ExtractionConfidence = CalculateConfidence(response),
            Images = images.ToArray(),
            ContentSize = extractedDataJson.Length
        };
    }

    /// <summary>
    /// Try to get a string value from dictionary with multiple possible keys
    /// </summary>
    private string? TryGetStringValue(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value))
            {
                return value?.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// Try to extract image URLs from data
    /// </summary>
    private List<string> TryGetImageList(Dictionary<string, object> data)
    {
        var images = new List<string>();

        // Try common image field names
        var imageKeys = new[] { "images", "image", "imageUrl", "image_url", "thumbnails", "thumbnail" };

        foreach (var key in imageKeys)
        {
            if (data.TryGetValue(key, out var value))
            {
                if (value is string strUrl && !string.IsNullOrWhiteSpace(strUrl))
                {
                    images.Add(strUrl);
                }
                else if (value is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                images.Add(item.GetString()!);
                            }
                        }
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        images.Add(jsonElement.GetString()!);
                    }
                }
            }
        }

        return images;
    }

    /// <summary>
    /// Calculate confidence score based on response quality
    /// </summary>
    private double CalculateConfidence(Crawl4AIResponse response)
    {
        if (!response.Success || response.Data.Count == 0)
            return 0.0;

        // High confidence for Crawl4AI since it uses LLM for extraction
        // Reduce slightly if no navigation was performed (simpler extraction)
        var baseConfidence = 0.90;

        if (response.NavigationResult != null && response.NavigationResult.PagesCollected > 1)
        {
            // Higher confidence with successful pagination
            baseConfidence = 0.95;
        }

        return baseConfidence;
    }

    /// <summary>
    /// Create a failed crawl result
    /// </summary>
    private CrawlResult CreateFailedResult(Guid jobId, string url, string errorMessage)
    {
        return new CrawlResult
        {
            Id = Guid.NewGuid(),
            CrawlJobId = jobId,
            Url = url,
            HttpStatusCode = 0,
            IsSuccess = false,
            CrawledAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            ExtractionConfidence = 0.0
        };
    }
}
