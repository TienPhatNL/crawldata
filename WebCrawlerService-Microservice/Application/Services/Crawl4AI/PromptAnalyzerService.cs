using WebCrawlerService.Application.Services.Crawl4AI.Models;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Implementation of prompt analyzer service
/// </summary>
public class PromptAnalyzerService : IPromptAnalyzerService
{
    private readonly IGeminiService _geminiService;
    private readonly IRepository<PromptHistory> _promptHistoryRepo;
    private readonly ILogger<PromptAnalyzerService> _logger;

    public PromptAnalyzerService(
        IGeminiService geminiService,
        IRepository<PromptHistory> promptHistoryRepo,
        ILogger<PromptAnalyzerService> logger)
    {
        _geminiService = geminiService;
        _promptHistoryRepo = promptHistoryRepo;
        _logger = logger;
    }

    public async Task<PromptAnalysisResult> AnalyzePromptAsync(
        string prompt,
        string? url = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing prompt: {Prompt}", prompt);

            var analysisPrompt = $@"
Analyze this web crawling request and extract structured information.

User Prompt: ""{prompt}""
Target URL: {url ?? "Not provided"}

Extract the following information and return as JSON:
{{
  ""intent"": ""<extract_prices | navigate_and_extract | analyze_data | visualize_data>"",
  ""entities"": {{
    ""brand"": ""<brand name if mentioned>"",
    ""category"": ""<category if mentioned>"",
    ""product_type"": ""<product type if mentioned>""
  }},
  ""target_category"": ""<category name or null>"",
  ""target_brand"": ""<brand name or null>"",
  ""data_type"": ""<prices | products | reviews | images | etc>"",
  ""requires_navigation"": <true if needs to navigate/filter/select categories, false if direct extraction>,
  ""confidence"": <0.0 to 1.0>
}}

Examples:
1. ""Crawl all iPhone prices from Electronics category"" →
   {{ ""intent"": ""navigate_and_extract"", ""entities"": {{ ""brand"": ""iPhone"", ""category"": ""Electronics"" }}, ""target_category"": ""Electronics"", ""target_brand"": ""iPhone"", ""data_type"": ""prices"", ""requires_navigation"": true, ""confidence"": 0.95 }}

2. ""Extract product names and prices from this page"" →
   {{ ""intent"": ""extract_prices"", ""entities"": {{}}, ""data_type"": ""prices"", ""requires_navigation"": false, ""confidence"": 1.0 }}

3. ""Get all brand A products"" →
   {{ ""intent"": ""navigate_and_extract"", ""entities"": {{ ""brand"": ""brand A"" }}, ""target_brand"": ""brand A"", ""data_type"": ""products"", ""requires_navigation"": true, ""confidence"": 0.9 }}

Return ONLY the JSON, no other text.
";

            var result = await _geminiService.GenerateJsonAsync<PromptAnalysisResult>(
                analysisPrompt,
                cancellationToken
            );

            if (result == null)
            {
                _logger.LogWarning("Failed to parse prompt analysis, using defaults");
                result = new PromptAnalysisResult
                {
                    Intent = "extract_prices",
                    RequiresNavigation = false,
                    Confidence = 0.5
                };
            }

            _logger.LogInformation("Prompt analysis complete. Intent: {Intent}, Requires Navigation: {RequiresNavigation}",
                result.Intent, result.RequiresNavigation);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing prompt");

            // Return safe default
            return new PromptAnalysisResult
            {
                Intent = "extract_prices",
                RequiresNavigation = false,
                Confidence = 0.3
            };
        }
    }
}
