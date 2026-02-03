using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Infrastructure.Common;

namespace WebCrawlerService.Infrastructure.Services;

/// <summary>
/// LLM-based extraction service using Google Gemini Vision API
/// </summary>
public class LlmExtractionService : ILlmExtractionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmSettings _settings;
    private readonly ILogger<LlmExtractionService> _logger;

    // API endpoint
    private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    // Model pricing (per 1M tokens)
    private static readonly Dictionary<string, (decimal input, decimal output)> ModelPricing = new()
    {
        ["gemini-1.5-pro"] = (1.25m, 5.00m),
        ["gemini-1.5-flash"] = (0.075m, 0.30m)
    };

    public LlmExtractionService(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmSettings> settings,
        ILogger<LlmExtractionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractAsync(
        ScreenState screenState,
        ExtractionSchema schema,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build extraction prompt from schema
            var prompt = BuildExtractionPrompt(schema, screenState);

            // Call LLM
            var (success, data, tokensUsed, model, errorMessage) = await CallLlmAsync(
                prompt,
                screenState.Screenshot,
                cancellationToken);

            stopwatch.Stop();

            if (!success)
            {
                return new ExtractionResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    Duration = stopwatch.Elapsed,
                    ModelUsed = model
                };
            }

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(data);

            // Calculate cost
            var cost = CalculateCost(model, tokensUsed);

            return new ExtractionResult
            {
                Data = jsonDoc,
                Success = true,
                Confidence = ExtractConfidenceScore(jsonDoc),
                ModelUsed = model,
                TokensUsed = tokensUsed,
                EstimatedCost = cost,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM extraction");
            stopwatch.Stop();

            return new ExtractionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<ExtractionResult> ExtractWithPromptAsync(
        ScreenState screenState,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (success, data, tokensUsed, model, errorMessage) = await CallLlmAsync(
                prompt,
                screenState.Screenshot,
                cancellationToken);

            stopwatch.Stop();

            if (!success)
            {
                return new ExtractionResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    Duration = stopwatch.Elapsed,
                    ModelUsed = model
                };
            }

            var jsonDoc = JsonDocument.Parse(data);
            var cost = CalculateCost(model, tokensUsed);

            return new ExtractionResult
            {
                Data = jsonDoc,
                Success = true,
                Confidence = 0.9,
                ModelUsed = model,
                TokensUsed = tokensUsed,
                EstimatedCost = cost,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM extraction with custom prompt");
            stopwatch.Stop();

            return new ExtractionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<bool> ValidateScreenTypeAsync(
        ScreenState screenState,
        string expectedScreenType,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"
Analyze this mobile app screenshot and determine if it matches the expected screen type.

Expected screen type: {expectedScreenType}

Visible text on screen:
{string.Join("\n", screenState.VisibleText)}

Respond with only a JSON object:
{{
    ""is_match"": true/false,
    ""confidence"": 0.0-1.0,
    ""reasoning"": ""brief explanation""
}}
";

        try
        {
            var (success, data, _, _, _) = await CallLlmAsync(
                prompt,
                screenState.Screenshot,
                cancellationToken);

            if (!success) return false;

            var result = JsonDocument.Parse(data);
            return result.RootElement.GetProperty("is_match").GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating screen type");
            return false;
        }
    }

    public async Task<string> DetermineNextActionAsync(
        ScreenState screenState,
        string goal,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"
You are an AI agent controlling a mobile app to accomplish a goal.

Current goal: {goal}

Visible text on screen:
{string.Join("\n", screenState.VisibleText)}

Based on the screenshot and UI elements, determine the next action to take.

Respond with only a JSON object:
{{
    ""action"": ""tap|scroll|swipe|back|input_text"",
    ""target"": ""description of element to interact with or text to input"",
    ""reasoning"": ""why this action helps achieve the goal""
}}
";

        try
        {
            var (success, data, _, _, _) = await CallLlmAsync(
                prompt,
                screenState.Screenshot,
                cancellationToken);

            if (!success) return "back"; // Default fallback action

            var result = JsonDocument.Parse(data);
            var action = result.RootElement.GetProperty("action").GetString();
            var target = result.RootElement.GetProperty("target").GetString();

            return $"{action}:{target}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining next action");
            return "back";
        }
    }

    /// <summary>
    /// Builds an extraction prompt from schema and screen state
    /// </summary>
    private string BuildExtractionPrompt(ExtractionSchema schema, ScreenState screenState)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Extract structured data from this mobile app screenshot.");
        sb.AppendLine();
        sb.AppendLine($"Task: {schema.Description}");
        sb.AppendLine();
        sb.AppendLine("Required fields:");

        foreach (var (fieldName, fieldSchema) in schema.Fields)
        {
            var required = fieldSchema.Required ? "(required)" : "(optional)";
            sb.AppendLine($"  - {fieldName}: {fieldSchema.Type} {required} - {fieldSchema.Description}");

            if (!string.IsNullOrEmpty(fieldSchema.Example))
            {
                sb.AppendLine($"    Example: {fieldSchema.Example}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Visible text on screen:");
        sb.AppendLine(string.Join("\n", screenState.VisibleText.Take(50))); // Limit to reduce tokens

        sb.AppendLine();
        sb.AppendLine("Response format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"confidence\": 0.0-1.0,");
        sb.AppendLine("  \"data\": {");

        var fieldNames = schema.Fields.Keys.ToList();
        for (int i = 0; i < fieldNames.Count; i++)
        {
            var fieldName = fieldNames[i];
            var fieldSchema = schema.Fields[fieldName];
            var comma = i < fieldNames.Count - 1 ? "," : "";

            var exampleValue = fieldSchema.Type switch
            {
                "string" => "\"example\"",
                "number" => "123",
                "boolean" => "true",
                "array" => "[]",
                _ => "null"
            };

            sb.AppendLine($"    \"{fieldName}\": {exampleValue}{comma}");
        }

        sb.AppendLine("  }");
        sb.AppendLine("}");

        sb.AppendLine();
        sb.AppendLine("Important:");
        sb.AppendLine("- Return ONLY valid JSON, no additional text");
        sb.AppendLine("- If data is not visible, use null");
        sb.AppendLine("- Include confidence score based on visibility and clarity");
        sb.AppendLine("- For prices, extract numeric value only (remove currency symbols)");

        return sb.ToString();
    }

    /// <summary>
    /// Calls Google Gemini Vision API for extraction
    /// </summary>
    private async Task<(bool success, string data, int tokens, string model, string? error)> CallLlmAsync(
        string prompt,
        string screenshotBase64,
        CancellationToken cancellationToken)
    {
        return await CallGeminiAsync(prompt, screenshotBase64, cancellationToken);
    }

    /// <summary>
    /// Calls Google Gemini Vision API
    /// </summary>
    private async Task<(bool success, string data, int tokens, string model, string? error)> CallGeminiAsync(
        string prompt,
        string screenshotBase64,
        CancellationToken cancellationToken)
    {
        var model = _settings.GeminiModel ?? "gemini-1.5-pro";

        // Gemini uses a different API structure
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/png",
                                data = screenshotBase64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = _settings.MaxTokens,
                responseMimeType = "application/json"
            }
        };

        using var client = _httpClientFactory.CreateClient();

        // Gemini uses API key in query string
        var url = $"{GeminiApiUrl}/{model}:generateContent?key={_settings.GeminiApiKey}";

        var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API error: {Error}", error);
            return (false, string.Empty, 0, model, error);
        }

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);

        // Extract content from Gemini response format
        var content = result!.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()!;

        // Extract token usage
        var usageMetadata = result.RootElement.GetProperty("usageMetadata");
        var promptTokens = usageMetadata.GetProperty("promptTokenCount").GetInt32();
        var candidatesTokens = usageMetadata.GetProperty("candidatesTokenCount").GetInt32();
        var totalTokens = promptTokens + candidatesTokens;

        return (true, content, totalTokens, model, null);
    }

    /// <summary>
    /// Calculates estimated cost based on model and token usage
    /// </summary>
    private decimal CalculateCost(string model, int tokens)
    {
        if (!ModelPricing.TryGetValue(model, out var pricing))
        {
            return 0m;
        }

        // Rough estimate: assume 60% input, 40% output
        var inputTokens = tokens * 0.6m;
        var outputTokens = tokens * 0.4m;

        var cost = (inputTokens * pricing.input / 1_000_000m) +
                   (outputTokens * pricing.output / 1_000_000m);

        return Math.Round(cost, 6);
    }

    /// <summary>
    /// Extracts confidence score from LLM response JSON
    /// </summary>
    private double ExtractConfidenceScore(JsonDocument data)
    {
        try
        {
            if (data.RootElement.TryGetProperty("confidence", out var confidenceProp))
            {
                return confidenceProp.GetDouble();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 0.8; // Default confidence
    }
}
