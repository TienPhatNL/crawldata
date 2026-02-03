using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ClassroomService.Infrastructure.Services;

public class CrawlerIntegrationService : ICrawlerIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CrawlerIntegrationService> _logger;
    private readonly string _crawlerServiceBaseUrl;

    public CrawlerIntegrationService(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        ILogger<CrawlerIntegrationService> _logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
        _crawlerServiceBaseUrl = configuration["Services:WebCrawlerService:BaseUrl"]
            ?? throw new InvalidOperationException("Services:WebCrawlerService:BaseUrl not configured");

        _httpClient.BaseAddress = new Uri(_crawlerServiceBaseUrl);
    }

    public async Task<CrawlJobResponse> InitiateCrawlAsync(InitiateCrawlRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Initiating crawl for Assignment {AssignmentId}, Group {GroupId}, User {UserId}",
                request.AssignmentId, request.GroupId, request.UserId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/crawler/initiate",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CrawlJobResponse>(cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize crawl job response");
            }

            _logger.LogInformation("Crawl job {JobId} initiated successfully", result.JobId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while initiating crawl: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to communicate with WebCrawlerService", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while initiating crawl");
            throw;
        }
    }

    public async Task<CrawlJobResponse> InitiateSmartCrawlAsync(SmartCrawlRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Initiating smart crawl for URL {Url}, User {UserId}, Conversation {ConversationId}",
                request.Url, request.UserId, request.ConversationThreadId);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/smart-crawler/crawl",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CrawlJobResponse>(cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize smart crawl job response");
            }

            _logger.LogInformation("Smart crawl job {JobId} initiated successfully", result.JobId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while initiating smart crawl: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to communicate with WebCrawlerService for smart crawl", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while initiating smart crawl");
            throw;
        }
    }

    public async Task<string?> AskQuestionAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch uploaded CSV files for this conversation
            var uploadedFiles = await _unitOfWork.ConversationUploadedFiles
                .GetByConversationIdOrderedAsync(conversationId, cancellationToken);
            
            // Format CSV data as context
            var csvContext = FormatCsvDataAsContext(uploadedFiles);
            
            var requestBody = new 
            { 
                ConversationId = conversationId, 
                Question = question,
                CsvContext = !string.IsNullOrWhiteSpace(csvContext) ? csvContext : null
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/crawler/ask",
                requestBody,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Use ReadAsStringAsync to avoid JSON deserialization limits
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    MaxDepth = 128,  // Increase from default 64
                    DefaultBufferSize = 32 * 1024  // 32KB buffer for large responses
                };
                
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString, options);
                return result != null && result.TryGetValue("answer", out var answer) ? answer : null;
            }
            
            _logger.LogWarning("Failed to ask question. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking question to WebCrawlerService");
            return null;
        }
    }

    /// <summary>
    /// Formats uploaded CSV files as readable text context for AI processing
    /// </summary>
    private string FormatCsvDataAsContext(List<ConversationUploadedFile> uploadedFiles)
    {
        if (!uploadedFiles.Any())
            return string.Empty;
        
        var allProducts = new List<object>();
        
        foreach (var file in uploadedFiles.OrderByDescending(f => f.UploadedAt))
        {
            try
            {
                var columns = JsonSerializer.Deserialize<List<string>>(file.ColumnNamesJson) ?? new List<string>();
                var rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(file.DataJson) ?? new List<Dictionary<string, string>>();
                
                if (!rows.Any())
                    continue;
                
                // Convert each CSV row to JSON object with source metadata
                var maxRows = Math.Min(500, rows.Count); // Match MaxResultsPerJob from SmartCrawlerOrchestrationService
                foreach (var row in rows.Take(maxRows))
                {
                    // Create product object with source metadata + CSV data
                    var product = new Dictionary<string, object>
                    {
                        // Source metadata (same format as crawl jobs)
                        ["_source"] = "csv_file",
                        ["_crawl_job_id"] = file.Id.ToString(),
                        ["_source_url"] = file.FileName, // Use filename as source identifier
                        ["_job_prompt"] = $"Uploaded file: {file.FileName}"
                    };
                    
                    // Add all CSV columns as product fields
                    foreach (var col in columns)
                    {
                        product[col] = row.GetValueOrDefault(col, "") ?? "";
                    }
                    
                    allProducts.Add(product);
                }
                
                _logger.LogInformation(
                    "📄 Formatted CSV file {FileName}: {Count} rows with source metadata (_source=csv_file, _source_url={FileName})",
                    file.FileName, Math.Min(maxRows, rows.Count), file.FileName);
            }
            catch (Exception ex)
            {
                // Skip malformed JSON
                _logger.LogWarning(ex, "Failed to parse CSV data for file {FileName}", file.FileName);
                continue;
            }
        }
        
        if (!allProducts.Any())
            return string.Empty;
        
        // Return JSON array of products (same format as crawl results)
        return JsonSerializer.Serialize(allProducts, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public async Task<CrawlJobStatusResponse> GetCrawlStatusAsync(Guid crawlJobId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CrawlJobStatusResponse>(
                $"/api/crawler/{crawlJobId}/status?userId={userId}",
                cancellationToken);

            if (response == null)
            {
                throw new InvalidOperationException($"Crawl job {crawlJobId} not found");
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting crawl status for {JobId}", crawlJobId);
            throw new InvalidOperationException("Failed to get crawl status", ex);
        }
    }

    public async Task<List<CrawlResultDetailDto>> GetCrawlResultsAsync(Guid crawlJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<CrawlResultDetailDto>>(
                $"/api/smart-crawler/job/{crawlJobId}/results",
                cancellationToken);

            return response ?? new List<CrawlResultDetailDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting crawl results for {JobId}", crawlJobId);
            throw new InvalidOperationException("Failed to get crawl results", ex);
        }
    }

    public async Task<bool> CancelCrawlAsync(Guid crawlJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/crawler/{crawlJobId}/cancel",
                null,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while cancelling crawl {JobId}", crawlJobId);
            return false;
        }
    }

    public async Task<List<CrawlJobStatusResponse>> GetAssignmentCrawlsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<CrawlJobStatusResponse>>(
                $"/api/crawler/assignment/{assignmentId}/jobs",
                cancellationToken);

            return response ?? new List<CrawlJobStatusResponse>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting assignment crawls for {AssignmentId}", assignmentId);
            return new List<CrawlJobStatusResponse>();
        }
    }

    public async Task<List<CrawlJobStatusResponse>> GetGroupCrawlsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<CrawlJobStatusResponse>>(
                $"/api/crawler/group/{groupId}/jobs",
                cancellationToken);

            return response ?? new List<CrawlJobStatusResponse>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while getting group crawls for {GroupId}", groupId);
            return new List<CrawlJobStatusResponse>();
        }
    }

    public async Task<bool> ShareCrawlWithGroupAsync(Guid crawlJobId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { GroupId = groupId }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"/api/crawler/{crawlJobId}/share",
                content,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while sharing crawl {JobId} with group {GroupId}", crawlJobId, groupId);
            return false;
        }
    }

    public async Task<string> GetCrawlSummaryAsync(Guid crawlJobId, CancellationToken cancellationToken = default)
    {
        var summary = await GetFullCrawlSummaryAsync(crawlJobId, null, null, cancellationToken);
        return summary?.SummaryText ?? string.Empty;
    }

    public async Task<CrawlJobDetailsDto?> GetCrawlJobAsync(Guid crawlJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/smart-crawler/job/{crawlJobId}";
            _logger.LogInformation("📡 Fetching job details for {JobId} from WebCrawlerService...", crawlJobId);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Failed to fetch job details for {JobId}. Status: {StatusCode}. Reason: {Error}",
                    crawlJobId, response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CrawlJobDetailsDto>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("⚠️ Job details response for {JobId} was null after deserialization", crawlJobId);
            }
            else
            {
                _logger.LogInformation("✅ Successfully retrieved job details for {JobId}, ConversationName: {ConversationName}",
                    crawlJobId, result.ConversationName ?? "<null>");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Exception calling WebCrawlerService for job {JobId}", crawlJobId);
            return null;
        }
    }

    public async Task<CrawlJobSummaryDto?> GetFullCrawlSummaryAsync(Guid crawlJobId, string? accessToken = null, string? prompt = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/analytics/jobs/{crawlJobId}/summary";
            if (!string.IsNullOrEmpty(prompt))
            {
                url += $"?prompt={Uri.EscapeDataString(prompt)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            _logger.LogInformation("📡 Fetching summary for Job {JobId} from WebCrawlerService... Prompt: {Prompt}", crawlJobId, prompt ?? "<none>");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Failed to fetch summary for Job {JobId}. Status: {StatusCode}. Reason: {Error}", 
                    crawlJobId, response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CrawlJobSummaryDto>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("⚠️ Summary response for Job {JobId} was null after deserialization", crawlJobId);
            }
            else
            {
                _logger.LogInformation("✅ Successfully retrieved summary for Job {JobId}", crawlJobId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Exception calling WebCrawlerService for Job {JobId}", crawlJobId);
            return null;
        }
    }

    /// <summary>
    /// NEW: summary/charts for the whole conversation (aggregates multiple jobs on WebCrawlerService)
    /// Endpoint: GET /api/analytics/conversations/{conversationId}/summary?prompt=...
    /// </summary>
    public async Task<CrawlJobSummaryDto?> GetConversationSummaryAsync(
        Guid conversationId,
        string? accessToken = null,
        string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/analytics/conversations/{conversationId}/summary";
            if (!string.IsNullOrEmpty(prompt))
            {
                url += $"?prompt={Uri.EscapeDataString(prompt)}";
            }
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            _logger.LogInformation("📡 Fetching conversation summary for Conversation {ConversationId}... Prompt: {Prompt}",
                conversationId, prompt ?? "<none>");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Failed to fetch conversation summary for {ConversationId}. Status: {StatusCode}. Reason: {Error}",
                    conversationId, response.StatusCode, errorContent);
                return null;
            }
            var result = await response.Content.ReadFromJsonAsync<CrawlJobSummaryDto>(cancellationToken: cancellationToken);
            if (result == null)
            {
                _logger.LogWarning("⚠️ Conversation summary response for {ConversationId} was null after deserialization", conversationId);
            }
            else
            {
                _logger.LogInformation("✅ Successfully retrieved conversation summary for {ConversationId}", conversationId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Exception calling WebCrawlerService for conversation summary {ConversationId}", conversationId);
            return null;
        }
    }
}
