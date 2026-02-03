using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebCrawlerService.Application.Configuration;
using WebCrawlerService.Application.Services.Crawl4AI.Models;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Domain.Models.Crawl4AI;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Implementation of crawl4ai client service
/// </summary>
public class Crawl4AIClientService : ICrawl4AIClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Crawl4AIClientService> _logger;
    private static int _instanceIndex = 0;
    private readonly IReadOnlyList<Uri> _agentEndpoints;

    // JSON options for case-insensitive deserialization (Python uses snake_case)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Removed to allow exact matching with JsonPropertyName attributes
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private static string TruncateForLog(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static string BuildPromptPreview(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "<empty>";
        }

        var normalized = prompt.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..157] + "...";
    }

    public Crawl4AIClientService(
        IHttpClientFactory httpClientFactory,
        IOptions<Crawl4AIOptions> crawl4AIOptions,
        ILogger<Crawl4AIClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var options = crawl4AIOptions.Value ?? new Crawl4AIOptions();

        var agents = options.Agents
            .Where(a => !string.IsNullOrWhiteSpace(a.Url))
            .Select(a => new Uri(a.Url!, UriKind.Absolute))
            .ToList();

        if (agents.Count == 0 && !string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            agents.Add(new Uri(options.BaseUrl, UriKind.Absolute));
        }

        if (agents.Count == 0)
        {
            throw new InvalidOperationException("No Crawl4AI endpoints configured. Set Crawl4AI:BaseUrl or Crawl4AI:Agents[].Url in configuration.");
        }

        _agentEndpoints = agents;
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient("Crawl4AIClient");

        var endpointsCount = _agentEndpoints.Count;
        var index = Math.Abs(Interlocked.Increment(ref _instanceIndex)) % endpointsCount;
        var selectedEndpoint = _agentEndpoints[index];

        httpClient.BaseAddress = selectedEndpoint;
        _logger.LogDebug("Routing Crawl4AI request to {Endpoint}", selectedEndpoint);

        return httpClient;
    }

    public async Task<CrawlSubmissionResult> SubmitCrawlJobAsync(
        string url,
        string prompt,
        string jobId,
        string userId,
        List<NavigationStep>? navigationSteps = null,
        int? maxPages = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting crawl job {JobId} to crawl4ai (fire-and-forget): {Url}", jobId, url);

            var request = new Crawl4AIRequest
            {
                Url = url,
                Prompt = prompt,
                JobId = jobId,
                UserId = userId,
                NavigationSteps = navigationSteps?.Select(s => new Dictionary<string, object>
                {
                    ["action"] = s.Action,
                    ["selector"] = s.Target ?? "",
                    ["value"] = s.Parameters?.GetValueOrDefault("value") ?? "",
                    ["description"] = s.Description ?? ""
                }).ToList(),
                MaxPages = maxPages
            };

            _logger.LogDebug("Dispatching crawl4ai fire-and-forget job. JobId={JobId}, Url={Url}, PromptPreview={PromptPreview}, Steps={StepCount}, MaxPages={MaxPages}",
                jobId ?? "<none>", url, BuildPromptPreview(prompt), request.NavigationSteps?.Count ?? 0, maxPages?.ToString() ?? "null");

            // DEBUG: Serialize to see actual JSON being sent
            var debugJson = System.Text.Json.JsonSerializer.Serialize(request);
            _logger.LogInformation("🔍 DEBUG: Sending to Python: {JsonPayload}", debugJson);

            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync(
                "/crawl",
                request,
                cancellationToken
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation("Crawl job {JobId} accepted by crawl4ai agent (HTTP 202)", jobId);
                return CrawlSubmissionResult.Success(sync: false);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Crawl job {JobId} processed by crawl4ai agent (HTTP 200)", jobId);
                try 
                {
                    // DEBUG: Log raw JSON to see what Python actually returned
                    var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("🔍 DEBUG: Raw JSON from Python (first 500 chars): {Json}", 
                        rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson);
                    
                    var result = System.Text.Json.JsonSerializer.Deserialize<Crawl4AIResponse>(rawJson, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    _logger.LogInformation("🔍 DEBUG: Deserialized ConversationName: '{Name}' (null: {IsNull})", 
                        result?.ConversationName ?? "<NULL>", result?.ConversationName == null);
                    
                    return CrawlSubmissionResult.Success(sync: true, response: result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize synchronous response for job {JobId}", jobId);
                    return CrawlSubmissionResult.Failure($"Failed to parse response: {ex.Message}");
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("crawl4ai job submission failed. StatusCode={StatusCode}, Reason={ReasonPhrase}, JobId={JobId}, Url={Url}, Body={Body}",
                response.StatusCode,
                response.ReasonPhrase,
                jobId ?? "<none>",
                url,
                TruncateForLog(errorContent));
            return CrawlSubmissionResult.Failure($"Agent returned {response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting crawl job {JobId} to crawl4ai service", jobId);
            return CrawlSubmissionResult.Failure(ex.Message);
        }
    }

    public async Task<Crawl4AIResponse> IntelligentCrawlAsync(
        string url,
        string prompt,
        List<NavigationStep>? navigationSteps = null,
        string? jobId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending intelligent crawl request to crawl4ai: {Url}", url);
            _logger.LogDebug("Prompt: {Prompt}", prompt);

            if (!string.IsNullOrEmpty(jobId))
            {
                _logger.LogInformation("JobId: {JobId}, UserId: {UserId} (Kafka progress tracking enabled)", jobId, userId);
            }

            var request = new Crawl4AIRequest
            {
                Url = url,
                Prompt = prompt,
                JobId = jobId,
                UserId = userId,
                NavigationSteps = navigationSteps?.Select(s => new Dictionary<string, object>
                {
                    ["action"] = s.Action,
                    ["selector"] = s.Target ?? "",
                    ["value"] = s.Parameters?.GetValueOrDefault("value") ?? "",
                    ["description"] = s.Description ?? ""
                }).ToList(),
                MaxPages = null // Legacy synchronous method - uses default behavior
            };

            _logger.LogDebug("Sending intelligent crawl payload. JobId={JobId}, Url={Url}, PromptPreview={PromptPreview}, Steps={StepCount}",
                jobId ?? "<none>", url, BuildPromptPreview(prompt), request.NavigationSteps?.Count ?? 0);

            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync(
                "/crawl",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("crawl4ai intelligent crawl failed. StatusCode={StatusCode}, Reason={ReasonPhrase}, JobId={JobId}, Url={Url}, Body={Body}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    jobId ?? "<none>",
                    url,
                    TruncateForLog(errorContent));

                return new Crawl4AIResponse
                {
                    Success = false,
                    Error = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }

            Crawl4AIResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<Crawl4AIResponse>(
                    JsonOptions,
                    cancellationToken: cancellationToken
                );

                if (result == null)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Deserialization returned null. StatusCode={StatusCode}, JobId={JobId}, Url={Url}, Body={Body}",
                        response.StatusCode,
                        jobId ?? "<none>",
                        url,
                        TruncateForLog(responseContent));
                    throw new InvalidOperationException("Failed to deserialize crawl4ai response - result was null");
                }
            }
            catch (JsonException ex)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(ex, "JSON deserialization failed. StatusCode={StatusCode}, JobId={JobId}, Url={Url}, Body={Body}",
                    response.StatusCode,
                    jobId ?? "<none>",
                    url,
                    TruncateForLog(responseContent));
                throw new InvalidOperationException($"Failed to deserialize crawl4ai response: {ex.Message}", ex);
            }

            _logger.LogInformation("crawl4ai crawl completed. Success: {Success}, Items: {Count}, HasNavigation: {HasNavigation}",
                result.Success, result.Data.Count, result.NavigationResult != null);

            return result;
        }
        catch (InvalidOperationException)
        {
            // Re-throw deserialization errors as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling crawl4ai service for URL: {Url}", url);
            return new Crawl4AIResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<List<Dictionary<string, object>>> AnalyzePageAsync(
        string url,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing page structure: {Url}", url);

            var request = new
            {
                url,
                prompt
            };

            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync(
                "/analyze-page",
                request,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                cancellationToken: cancellationToken
            );

            if (result != null && result.TryGetValue("suggested_steps", out var steps))
            {
                var jsonSteps = JsonSerializer.Serialize(steps);
                var parsedSteps = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonSteps);
                return parsedSteps ?? new List<Dictionary<string, object>>();
            }

            return new List<Dictionary<string, object>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing page: {Url}", url);
            return new List<Dictionary<string, object>>();
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> AskQuestionAsync(string context, string question, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = GetHttpClient();
            var response = await httpClient.PostAsJsonAsync("/query", new
            {
                context,
                query = question
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
                return result != null && result.TryGetValue("answer", out var answer) ? answer : null;
            }
            
            _logger.LogError("Query failed with status {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking question to agent");
            return null;
        }
    }

    public async Task<SummaryResponse> GenerateSummaryAsync(
        string jobId,
        object data,
        string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting summary for job {JobId} with prompt: {Prompt}", jobId, prompt ?? "<none>");
            var httpClient = GetHttpClient();
            
            var request = new
            {
                job_id = jobId,
                data = data,
                source = "manual",
                prompt = prompt
            };

            var response = await httpClient.PostAsJsonAsync("/summary", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Summary generation failed. Status={Status}, Error={Error}", response.StatusCode, error);
                throw new HttpRequestException($"Summary generation failed: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("🐍 Python Agent Raw Response: {Content}", content);

            var result = JsonSerializer.Deserialize<SummaryResponse>(content, JsonOptions);
            return result ?? new SummaryResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary for job {JobId}", jobId);
            throw;
        }
    }
}
