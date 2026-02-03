using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WebCrawlerService.Application.Configuration;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

public class LlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly ILogger<LlmService> _logger;
    private readonly LlmConfiguration _config;

    public LlmService(
        IOptions<LlmConfiguration> config,
        ILogger<LlmService> logger)
    {
        _logger = logger;
        _config = config.Value;

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: _config.ModelId,
            apiKey: _config.ApiKey);

        _kernel = builder.Build();
    }

    public async Task<ExtractionStrategy> GenerateExtractionStrategyAsync(
        string userPrompt,
        string sampleHtml,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var systemPrompt = @"You are an expert web scraping assistant. Given a user's natural language request and sample HTML,
generate a JSON extraction strategy with CSS selectors or XPath expressions.

IMPORTANT:
1. Return ONLY valid JSON, no additional text
2. Provide multiple fallback selectors for each field
3. Indicate if JavaScript execution is required
4. Estimate confidence score (0.0 to 1.0)
5. Include warnings about potential issues

Response format:
{
  ""fields"": [
    {
      ""name"": ""productName"",
      ""type"": ""text"",
      ""required"": true,
      ""selectors"": [""h1.product-title"", "".product-name"", ""#product-name""],
      ""extractionMethod"": ""text"",
      ""attributeName"": null,
      ""postProcessing"": [""trim"", ""removeEmptyLines""]
    }
  ],
  ""dynamicSelectors"": {},
  ""requiresJavaScript"": false,
  ""scrollToBottom"": false,
  ""waitForSelectors"": [],
  ""confidence"": 0.85,
  ""estimatedTimeSeconds"": 3,
  ""warnings"": [""Price selector may change frequently""]
}";

            var userMessage = $@"User Request: {userPrompt}

Sample HTML (first 2000 chars):
{sampleHtml.Substring(0, Math.Min(2000, sampleHtml.Length))}

Generate extraction strategy:";

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                MaxTokens = 2000
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel,
                cancellationToken);

            var strategy = JsonSerializer.Deserialize<ExtractionStrategy>(
                response.Content ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return strategy ?? throw new InvalidOperationException("Failed to deserialize strategy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating extraction strategy");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> ExtractDataWithAiAsync(
        string html,
        ExtractionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var systemPrompt = @"You are a data extraction assistant. Extract the requested fields from HTML content.
Return ONLY valid JSON with the extracted data.";

            var fieldDescriptions = string.Join("\n", strategy.Fields.Select(f =>
                $"- {f.Name} ({f.Type}): {(f.Required ? "Required" : "Optional")}"));

            var userMessage = $@"Extract these fields:
{fieldDescriptions}

From this HTML (first 3000 chars):
{html.Substring(0, Math.Min(3000, html.Length))}

Return extracted data as JSON:";

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.1,
                MaxTokens = 1500
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel,
                cancellationToken);

            var extractedData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                response.Content ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return extractedData ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting data with AI");
            return new Dictionary<string, object>();
        }
    }

    public async Task<ExtractionStrategy> ValidateAndImproveStrategyAsync(
        ExtractionStrategy strategy,
        string sampleHtml,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var systemPrompt = @"You are a web scraping validator. Review the extraction strategy and improve it based on the HTML.
Return an improved strategy in JSON format.";

            var strategyJson = JsonSerializer.Serialize(strategy);

            var userMessage = $@"Current Strategy:
{strategyJson}

Sample HTML:
{sampleHtml.Substring(0, Math.Min(2000, sampleHtml.Length))}

Validate and improve the strategy. Return updated JSON:";

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3,
                MaxTokens = 2000
            };

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel,
                cancellationToken);

            var improvedStrategy = JsonSerializer.Deserialize<ExtractionStrategy>(
                response.Content ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return improvedStrategy ?? strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating strategy");
            return strategy;
        }
    }

    public Task<decimal> EstimateCostAsync(
        ExtractionStrategy strategy,
        int estimatedPageCount,
        CancellationToken cancellationToken = default)
    {
        // OpenAI GPT-4o-mini pricing (as of 2025)
        // Input: $0.15 per 1M tokens
        // Output: $0.60 per 1M tokens

        // Rough estimates:
        // - Strategy generation: ~1500 input tokens, ~500 output tokens per page
        // - Data extraction: ~2000 input tokens, ~300 output tokens per page

        var inputTokensPerPage = 2000;
        var outputTokensPerPage = 300;

        var totalInputTokens = inputTokensPerPage * estimatedPageCount;
        var totalOutputTokens = outputTokensPerPage * estimatedPageCount;

        var inputCost = (totalInputTokens / 1_000_000m) * 0.15m;
        var outputCost = (totalOutputTokens / 1_000_000m) * 0.60m;

        var totalCost = inputCost + outputCost;

        return Task.FromResult(totalCost);
    }
}
