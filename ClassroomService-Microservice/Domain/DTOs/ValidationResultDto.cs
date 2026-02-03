namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Result of data validation and cleaning process
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// List of successfully validated and cleaned records
    /// </summary>
    public List<object> ValidRecords { get; set; } = new();
    
    /// <summary>
    /// List of records that failed validation
    /// </summary>
    public List<object> InvalidRecords { get; set; } = new();
    
    /// <summary>
    /// Validation warnings and messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Data quality score (0.0 to 1.0)
    /// Calculated as ValidRecords.Count / (ValidRecords.Count + InvalidRecords.Count)
    /// </summary>
    public double QualityScore { get; set; }
}

/// <summary>
/// Detected data schema information
/// </summary>
public class DataSchema
{
    /// <summary>
    /// Type of data (e.g., "product_list", "article_list", "generic")
    /// </summary>
    public string Type { get; set; } = "generic";
    
    /// <summary>
    /// List of all detected fields across all records
    /// </summary>
    public List<string> Fields { get; set; } = new();
    
    /// <summary>
    /// Required fields that should be present in valid records
    /// </summary>
    public List<string> RequiredFields { get; set; } = new();
    
    /// <summary>
    /// Field to use as label in visualizations (e.g., "name", "title")
    /// </summary>
    public string? LabelField { get; set; }
    
    /// <summary>
    /// Field to use as value in visualizations (e.g., "price", "rating")
    /// </summary>
    public string? ValueField { get; set; }
    
    /// <summary>
    /// Field types mapping (field name -> type)
    /// </summary>
    public Dictionary<string, string> FieldTypes { get; set; } = new();
}
