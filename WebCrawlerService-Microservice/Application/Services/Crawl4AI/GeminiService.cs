using System.Text.Json;
using System.Net.Http.Json;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Implementation of Gemini LLM service
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["LlmSettings:GeminiApiKey"]
            ?? throw new InvalidOperationException("Gemini API key not configured");
        _model = configuration["LlmSettings:GeminiModel"] ?? "gemini-1.5-flash";
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("GeminiClient");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var response = await httpClient.PostAsJsonAsync(
                $"v1beta/models/{_model}:generateContent?key={_apiKey}",
                requestBody,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                cancellationToken: cancellationToken
            );

            if (result?.Candidates?.Length > 0 &&
                result.Candidates[0].Content?.Parts?.Length > 0)
            {
                return result.Candidates[0].Content.Parts[0].Text;
            }

            throw new InvalidOperationException("No content generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content with Gemini");
            throw;
        }
    }

    public async Task<T?> GenerateJsonAsync<T>(
        string prompt,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var response = await GenerateContentAsync(prompt, cancellationToken);

            // Extract JSON from markdown code blocks if present
            var jsonText = response.Trim();
            if (jsonText.Contains("```json"))
            {
                jsonText = jsonText.Split("```json")[1].Split("```")[0].Trim();
            }
            else if (jsonText.Contains("```"))
            {
                jsonText = jsonText.Split("```")[1].Split("```")[0].Trim();
            }

            return JsonSerializer.Deserialize<T>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON with Gemini");
            return null;
        }
    }

    // Response models for Gemini API
    private class GeminiResponse
    {
        public Candidate[]? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public Part[]? Parts { get; set; }
    }

    private class Part
    {
        public string Text { get; set; } = "";
    }
}
