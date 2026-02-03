using Microsoft.Extensions.Logging;
using WebCrawlerService.Application.Common.Interfaces;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;

namespace WebCrawlerService.Application.Services;

public class PromptAnalyzerService : IPromptAnalyzerService
{
    private readonly ILlmService _llmService;
    private readonly ICrawlTemplateRepository _templateRepository;
    private readonly ILogger<PromptAnalyzerService> _logger;

    // Keywords for identifying crawler requirements
    private readonly string[] _jsRequiredKeywords = new[]
    {
        "dynamic", "javascript", "react", "vue", "angular", "spa",
        "infinite scroll", "ajax", "load more", "click to load"
    };

    private readonly string[] _apiExtractKeywords = new[]
    {
        "api", "json", "rest", "graphql", "endpoint"
    };

    public PromptAnalyzerService(
        ILlmService llmService,
        ICrawlTemplateRepository templateRepository,
        ILogger<PromptAnalyzerService> logger)
    {
        _llmService = llmService;
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<CrawlerType> RecommendCrawlerTypeAsync(
        string userPrompt,
        string url,
        CancellationToken cancellationToken = default)
    {
        var promptLower = userPrompt.ToLower();

        // Check for API extraction keywords
        if (_apiExtractKeywords.Any(kw => promptLower.Contains(kw)))
        {
            return CrawlerType.AppSpecificApi;
        }

        // Check for JavaScript requirements
        if (_jsRequiredKeywords.Any(kw => promptLower.Contains(kw)))
        {
            return CrawlerType.Playwright;
        }

        // Check if URL matches known dynamic sites
        var dynamicDomains = new[] { "facebook", "instagram", "twitter", "linkedin", "shopee", "lazada" };
        if (dynamicDomains.Any(domain => url.Contains(domain, StringComparison.OrdinalIgnoreCase)))
        {
            return CrawlerType.Playwright;
        }

        // For simple extraction, use HTTP client
        if (promptLower.Contains("simple") || promptLower.Contains("basic") || promptLower.Contains("text only"))
        {
            return CrawlerType.HttpClient;
        }

        // Default to Playwright for reliability
        return CrawlerType.Playwright;
    }

    public async Task<CrawlTemplate?> FindMatchingTemplateAsync(
        string url,
        string? userPrompt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, try to find a template by domain pattern
            var templateByDomain = await _templateRepository.GetByDomainPatternAsync(url, cancellationToken);

            if (templateByDomain != null)
            {
                _logger.LogInformation("Found template by domain pattern: {TemplateName}", templateByDomain.Name);
                return templateByDomain;
            }

            // If user prompt provided, search templates by relevance
            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                var searchTerms = ExtractKeywords(userPrompt);
                foreach (var term in searchTerms)
                {
                    var templates = await _templateRepository.SearchTemplatesAsync(term, cancellationToken);
                    var matchingTemplate = templates.FirstOrDefault(t =>
                        url.Contains(t.DomainPattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase));

                    if (matchingTemplate != null)
                    {
                        _logger.LogInformation("Found template by search: {TemplateName}", matchingTemplate.Name);
                        return matchingTemplate;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding matching template");
            return null;
        }
    }

    public async Task<ExtractionStrategy> GenerateStrategyFromPromptAsync(
        string userPrompt,
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, we need sample HTML to generate strategy
            // This would typically be fetched by a quick HTTP request
            var sampleHtml = "<html><body>Sample content for strategy generation</body></html>";

            var strategy = await _llmService.GenerateExtractionStrategyAsync(
                userPrompt,
                sampleHtml,
                cancellationToken);

            _logger.LogInformation("Generated extraction strategy with {FieldCount} fields", strategy.Fields.Count);

            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating strategy from prompt");

            // Return a basic fallback strategy
            return new ExtractionStrategy
            {
                Confidence = 0.5,
                RequiresJavaScript = false,
                Fields = new List<FieldDefinition>(),
                Warnings = new[] { "Failed to generate strategy with AI, using fallback" }
            };
        }
    }

    public Task<(bool IsValid, string? ErrorMessage)> ValidatePromptAsync(
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return Task.FromResult((false, "Prompt cannot be empty"));
        }

        if (userPrompt.Length < 10)
        {
            return Task.FromResult((false, "Prompt is too short. Please provide more details"));
        }

        if (userPrompt.Length > 2000)
        {
            return Task.FromResult((false, "Prompt is too long. Maximum 2000 characters"));
        }

        // Check for prohibited keywords (data theft, credentials, etc.)
        var prohibitedKeywords = new[]
        {
            "password", "credit card", "ssn", "social security",
            "bank account", "private key", "secret", "token"
        };

        var promptLower = userPrompt.ToLower();
        var foundProhibited = prohibitedKeywords.FirstOrDefault(kw => promptLower.Contains(kw));

        if (foundProhibited != null)
        {
            return Task.FromResult((false, $"Prompt contains prohibited keyword: {foundProhibited}"));
        }

        return Task.FromResult((true, (string?)null));
    }

    private string[] ExtractKeywords(string prompt)
    {
        // Simple keyword extraction - split by spaces and filter
        var words = prompt.ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Distinct()
            .ToArray();

        return words;
    }
}
