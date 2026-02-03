using ClassroomService.Domain.Common;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// AI content detection service using ZeroGPT API
/// </summary>
public class ZeroGPTAIDetectionService : IAIDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZeroGPTAIDetectionService> _logger;
    private readonly AIDetectionSettings _settings;

    public ZeroGPTAIDetectionService(
        HttpClient httpClient,
        IOptions<AIDetectionSettings> settings,
        ILogger<ZeroGPTAIDetectionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeout);
        // ZeroGPT uses a custom header "ApiKey" instead of standard Authorization Bearer
        _httpClient.DefaultRequestHeaders.Add("ApiKey", _settings.ApiKey);
    }

    public async Task<(bool Success, decimal? AIPercentage, string? ErrorMessage, string? RawResponse)> 
        CheckContentAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate content
            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, "Content cannot be empty", null);
            }

            // Strip HTML tags and decode entities
            content = StripHtml(content);

            // Re-validate after stripping
            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, null, "Content contains only HTML tags or whitespace", null);
            }

            // Truncate content if too long
            if (content.Length > _settings.MaxContentLength)
            {
                _logger.LogWarning("Content truncated from {Original} to {Max} characters", 
                    content.Length, _settings.MaxContentLength);
                content = content.Substring(0, _settings.MaxContentLength);
            }

            // Prepare request
            var requestBody = new { input_text = content };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending AI detection request to ZeroGPT (content length: {Length})", 
                content.Length);

            // Make API call
            var response = await _httpClient.PostAsync(_settings.ApiEndpoint, httpContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZeroGPT API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                return (false, null, $"API error: {response.StatusCode}", responseContent);
            }

            // Parse response
            var result = ParseZeroGPTResponse(responseContent);

            if (result.HasValue)
            {
                _logger.LogInformation("AI detection completed: {Percentage}% AI-generated", result.Value);
                return (true, result.Value, null, responseContent);
            }
            else
            {
                _logger.LogError("Failed to parse ZeroGPT response: {Response}", responseContent);
                return (false, null, "Failed to parse AI detection response", responseContent);
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI detection request timed out");
            return (false, null, "Request timed out", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during AI detection");
            return (false, null, $"Network error: {ex.Message}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI detection");
            return (false, null, $"Unexpected error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Strips HTML tags and decodes entities to produce plain text
    /// </summary>
    private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // 0. Remove script and style elements with their content
        var scriptStyleRegex = new Regex(@"<script[^>]*>[\s\S]*?</script>|<style[^>]*>[\s\S]*?</style>", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var text = scriptStyleRegex.Replace(input, string.Empty);

        // 1. Replace block-level tags with newlines to preserve structure
        // We target the closing tags of blocks to ensure separation
        // Added img and hr (void tags) to ensure they create line breaks
        var blockTags = new Regex(@"(<br\s*/?>)|(<img[^>]*>)|(<hr\s*/?>)|(</p>)|(</div>)|(</li>)|(</tr>)|(</table>)|(</ul>)|(</ol>)|(</h1>)|(</h2>)|(</h3>)|(</h4>)|(</h5>)|(</h6>)|(</pre>)|(</blockquote>)|(</article>)|(</section>)|(</main>)|(</header>)|(</footer>)|(</nav>)|(</aside>)", 
            RegexOptions.IgnoreCase);
        text = blockTags.Replace(text, "\n");

        // 2. Handle table cells - add space between cells to prevent merging
        var cellTags = new Regex(@"(</td>)|(</th>)", RegexOptions.IgnoreCase);
        text = cellTags.Replace(text, " ");

        // 3. Remove all other HTML tags
        // We replace with empty string to preserve word connectivity (e.g., "Word<b>Bold</b>" -> "WordBold")
        // and punctuation attachment (e.g., "<b>Bold</b>." -> "Bold.")
        var allTags = new Regex(@"<[^>]+>");
        text = allTags.Replace(text, string.Empty);

        // 4. Decode HTML entities (e.g., &nbsp;, &amp;, &lt;)
        text = WebUtility.HtmlDecode(text);

        // 5. Normalize whitespace
        // Replace non-breaking spaces with regular spaces
        text = text.Replace('\u00A0', ' ');
        
        // Replace multiple spaces/tabs with single space
        text = Regex.Replace(text, @"[ \t]+", " ");

        // Split into lines, trim each line, and remove empty lines
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l));
        
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Parses ZeroGPT response to extract AI percentage
    /// </summary>
    private decimal? ParseZeroGPTResponse(string jsonResponse)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            // Check success flag
            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                if (root.TryGetProperty("data", out var dataProp) && 
                    dataProp.TryGetProperty("fakePercentage", out var percentageProp))
                {
                    // fakePercentage can be a number or a string in JSON
                    if (percentageProp.ValueKind == JsonValueKind.Number)
                    {
                        return percentageProp.GetDecimal();
                    }
                    else if (percentageProp.ValueKind == JsonValueKind.String)
                    {
                        if (decimal.TryParse(percentageProp.GetString(), out var percentage))
                        {
                            return percentage;
                        }
                    }
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing ZeroGPT response");
            return null;
        }
    }
}
