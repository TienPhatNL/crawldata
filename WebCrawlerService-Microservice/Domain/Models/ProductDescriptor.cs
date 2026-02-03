namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Lightweight description of a product extracted from crawl results.
/// </summary>
public sealed class ProductDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public string? SourceUrl { get; init; }
    public string? AdditionalNotes { get; init; }

    public override string ToString()
    {
        var parts = new List<string> { Name };
        if (!string.IsNullOrWhiteSpace(Brand)) parts.Add(Brand!);
        if (Price.HasValue) parts.Add($"{Price.Value:0} {Currency ?? "VND"}");
        if (!string.IsNullOrWhiteSpace(AdditionalNotes)) parts.Add(AdditionalNotes!);
        return string.Join(" ", parts);
    }
}
