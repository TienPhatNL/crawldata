namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Configuration for a crawl template
/// </summary>
public class TemplateConfiguration
{
    /// <summary>
    /// CSS/XPath selectors for each field
    /// </summary>
    public Dictionary<string, string> Selectors { get; set; } = new();

    /// <summary>
    /// Sample URLs for testing this template
    /// </summary>
    public string[] SampleUrls { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Wait conditions before extraction
    /// </summary>
    public WaitCondition? WaitConditions { get; set; }

    /// <summary>
    /// Pagination configuration
    /// </summary>
    public PaginationConfig? Pagination { get; set; }

    /// <summary>
    /// Custom HTTP headers
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Whether JavaScript execution is required
    /// </summary>
    public bool RequiresJavaScript { get; set; }

    /// <summary>
    /// Whether to scroll to bottom
    /// </summary>
    public bool ScrollToBottom { get; set; }

    /// <summary>
    /// Whether proxy is required for this template
    /// </summary>
    public bool ProxyRequired { get; set; }

    /// <summary>
    /// Geographic region for proxy (VN, US, etc.)
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Post-extraction data transformations
    /// </summary>
    public Dictionary<string, DataTransform>? Transformations { get; set; }
}

public class WaitCondition
{
    public string Selector { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 5000;
    public string State { get; set; } = "visible"; // visible, hidden, attached, detached
}

public class PaginationConfig
{
    public bool Enabled { get; set; }
    public string? NextButtonSelector { get; set; }
    public int MaxPages { get; set; } = 1;
    public int DelayBetweenPagesMs { get; set; } = 1000;
}

public class DataTransform
{
    public string Type { get; set; } = string.Empty; // trim, lowercase, uppercase, regex, parseNumber, parseDate
    public string? Pattern { get; set; }
    public string? Replacement { get; set; }
}
