namespace WebCrawlerService.Domain.Models;

/// <summary>
/// Defines the schema for data extraction from mobile app screens
/// </summary>
public class ExtractionSchema
{
    /// <summary>
    /// Name of the extraction schema (e.g., "product_details", "review_list")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what data to extract
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fields to extract with their types and descriptions
    /// </summary>
    public Dictionary<string, FieldSchema> Fields { get; set; } = new();

    /// <summary>
    /// Whether this extraction can return multiple items (e.g., list of reviews)
    /// </summary>
    public bool IsArray { get; set; } = false;
}

/// <summary>
/// Schema for a single field to extract
/// </summary>
public class FieldSchema
{
    /// <summary>
    /// JSON type: "string", "number", "boolean", "array", "object"
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Description to help LLM understand what to extract
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this field is required
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Example value to guide extraction
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// For nested objects or arrays
    /// </summary>
    public Dictionary<string, FieldSchema>? Properties { get; set; }
}
