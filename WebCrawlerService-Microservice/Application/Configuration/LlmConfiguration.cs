namespace WebCrawlerService.Application.Configuration;

public class LlmConfiguration
{
    public string Provider { get; set; } = "OpenAI"; // OpenAI, Claude, etc.
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o-mini"; // Default to cost-effective model
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.2;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 60;
}
