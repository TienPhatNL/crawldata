namespace WebCrawlerService.Application.Configuration;

/// <summary>
/// Options for Google Custom Search integration.
/// </summary>
public sealed class GoogleSearchOptions
{
    public const string SectionName = "Services:GoogleSearch";

    public string ApiKey { get; init; } = string.Empty;
    public string SearchEngineId { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://www.googleapis.com/customsearch/v1";
    public string DefaultCountry { get; init; } = "vn";
    public string DefaultLanguage { get; init; } = "lang_vi";
    public int MaxResultsPerQuery { get; init; } = 5;
}
