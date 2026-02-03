namespace WebCrawlerService.Application.DTOs.DataVisualization;

/// <summary>
/// Schema information for extracted data
/// </summary>
public class DataSchema
{
    /// <summary>
    /// Detected fields/columns in the data
    /// </summary>
    public List<DataField> Fields { get; set; } = new();

    /// <summary>
    /// Total number of records analyzed
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Whether the data is structured (consistent fields across records)
    /// </summary>
    public bool IsStructured { get; set; }

    /// <summary>
    /// Detected relationships between fields
    /// </summary>
    public List<string> Relationships { get; set; } = new();
}

/// <summary>
/// Information about a single data field
/// </summary>
public class DataField
{
    /// <summary>
    /// Name of the field
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detected data type (string, number, date, boolean, array, object)
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Semantic type for visualization (category, quantity, price, rating, percentage, url, etc.)
    /// </summary>
    public string SemanticType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this field is present in all records
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Number of unique values (for categorical fields)
    /// </summary>
    public int? UniqueValueCount { get; set; }

    /// <summary>
    /// Sample values from this field
    /// </summary>
    public List<object> SampleValues { get; set; } = new();

    /// <summary>
    /// Statistical info for numeric fields
    /// </summary>
    public NumericStats? NumericStatistics { get; set; }

    /// <summary>
    /// Percentage of records containing this field (0-100)
    /// </summary>
    public double Coverage { get; set; }

    /// <summary>
    /// Whether this field is suitable for visualization
    /// </summary>
    public bool IsVisualizable { get; set; }

    /// <summary>
    /// Suggested chart roles for this field (x-axis, y-axis, category, value, etc.)
    /// </summary>
    public List<string> SuggestedRoles { get; set; } = new();
}

/// <summary>
/// Statistical information for numeric fields
/// </summary>
public class NumericStats
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public double Median { get; set; }
    public double StandardDeviation { get; set; }
    public int Count { get; set; }
}
