using System.Collections.Generic;

namespace WebCrawlerService.Application.Configuration;

public class Crawl4AIOptions
{
    public const string SectionName = "Crawl4AI";

    /// <summary>
    /// Default base URL when no agent list is supplied.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8004";

    /// <summary>
    /// Optional list of individual agent instances to round-robin across.
    /// </summary>
    public List<Crawl4AIInstanceOptions> Agents { get; set; } = new();
}

public class Crawl4AIInstanceOptions
{
    public string? InstanceId { get; set; }
    public string? Url { get; set; }
}
