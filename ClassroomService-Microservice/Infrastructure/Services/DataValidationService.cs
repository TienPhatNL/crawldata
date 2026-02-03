using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for validating and cleaning crawl data
/// Handles missing fields, type mismatches, and malformed data
/// </summary>
public class DataValidationService : IDataValidator
{
    private readonly ILogger<DataValidationService> _logger;

    public DataValidationService(ILogger<DataValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAndCleanAsync(object rawData)
    {
        var result = new ValidationResult();

        try
        {
            // Parse JSON array
            var jsonString = JsonSerializer.Serialize(rawData);
            var items = JsonSerializer.Deserialize<JsonElement>(jsonString);

            // Handle single object - wrap in array
            if (items.ValueKind != JsonValueKind.Array)
            {
                result.Warnings.Add("Expected array, got single object - wrapping in array");
                items = JsonDocument.Parse($"[{jsonString}]").RootElement;
            }

            // Validate and clean each record
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var cleaned = CleanRecord(item);

                    if (IsValidRecord(cleaned))
                    {
                        result.ValidRecords.Add(cleaned);
                    }
                    else
                    {
                        result.InvalidRecords.Add(cleaned);
                        result.Warnings.Add($"Record validation failed: {JsonSerializer.Serialize(cleaned)}");
                    }
                }
                catch (Exception ex)
                {
                    result.InvalidRecords.Add(item.ToString());
                    result.Warnings.Add($"Parse error: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to parse record");
                }
            }

            // Calculate quality score
            result.QualityScore = result.ValidRecords.Count > 0
                ? (double)result.ValidRecords.Count / (result.ValidRecords.Count + result.InvalidRecords.Count)
                : 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate data");
            result.Warnings.Add($"Validation failed: {ex.Message}");
            result.QualityScore = 0.0;
        }

        return await Task.FromResult(result);
    }

    public DataSchema DetectSchema(List<object> records)
    {
        var schema = new DataSchema();

        if (!records.Any())
            return schema;

        var allFields = new HashSet<string>();
        var fieldTypes = new Dictionary<string, Dictionary<string, int>>(); // field -> type -> count
        var fieldOccurrences = new Dictionary<string, int>(); // field -> occurrence count

        // Analyze all records
        foreach (var record in records)
        {
            var dict = record as Dictionary<string, object?> 
                ?? JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    JsonSerializer.Serialize(record)) 
                ?? new Dictionary<string, object?>();

            foreach (var kvp in dict)
            {
                allFields.Add(kvp.Key);
                
                // Count field occurrences
                fieldOccurrences[kvp.Key] = fieldOccurrences.GetValueOrDefault(kvp.Key, 0) + 1;

                // Detect type
                var type = DetectFieldType(kvp.Value);
                if (!fieldTypes.ContainsKey(kvp.Key))
                    fieldTypes[kvp.Key] = new Dictionary<string, int>();
                
                fieldTypes[kvp.Key][type] = fieldTypes[kvp.Key].GetValueOrDefault(type, 0) + 1;
            }
        }

        schema.Fields = allFields.ToList();

        // Determine required fields (appear in >80% of records)
        var threshold = records.Count * 0.8;
        schema.RequiredFields = fieldOccurrences
            .Where(kvp => kvp.Value >= threshold)
            .Select(kvp => kvp.Key)
            .ToList();

        // Determine field types (most common type for each field)
        foreach (var field in schema.Fields)
        {
            if (fieldTypes.ContainsKey(field))
            {
                var mostCommonType = fieldTypes[field]
                    .OrderByDescending(t => t.Value)
                    .First()
                    .Key;
                schema.FieldTypes[field] = mostCommonType;
            }
        }

        // Auto-detect schema type and key fields
        DetectSchemaTypeAndKeyFields(schema);

        return schema;
    }

    private Dictionary<string, object?> CleanRecord(JsonElement item)
    {
        var cleaned = new Dictionary<string, object?>();

        foreach (var prop in item.EnumerateObject())
        {
            var value = prop.Value;

            // Handle missing/null values
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                cleaned[prop.Name] = GetDefaultValue(prop.Name);
                continue;
            }

            // Type coercion based on field name and value
            cleaned[prop.Name] = value.ValueKind switch
            {
                JsonValueKind.String => CoerceString(prop.Name, value.GetString()),
                JsonValueKind.Number => value.TryGetDecimal(out var dec) ? dec : value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => value.EnumerateArray().Select(e => e.ToString()).ToArray(),
                JsonValueKind.Object => value.ToString(),
                _ => value.ToString()
            };
        }

        return cleaned;
    }

    private object? CoerceString(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GetDefaultValue(fieldName);

        var lowerField = fieldName.ToLower();

        // Detect numeric fields (price, rating, quantity, stock)
        if (lowerField.Contains("price") || lowerField.Contains("cost") || lowerField.Contains("amount"))
        {
            // Remove currency symbols and thousands separators
            var cleaned = Regex.Replace(value, @"[^\d\.]", "");
            return decimal.TryParse(cleaned, out var price) ? price : 0m;
        }

        if (lowerField.Contains("rating") || lowerField.Contains("score"))
        {
            var cleaned = Regex.Replace(value, @"[^\d\.]", "");
            return double.TryParse(cleaned, out var rating) ? rating : 0.0;
        }

        if (lowerField.Contains("quantity") || lowerField.Contains("stock") || lowerField.Contains("count"))
        {
            var cleaned = Regex.Replace(value, @"[^\d]", "");
            return int.TryParse(cleaned, out var qty) ? qty : 0;
        }

        // Try to parse as number if it looks numeric
        if (Regex.IsMatch(value, @"^\d+[\d\.,]*$"))
        {
            var cleaned = Regex.Replace(value, @"[^\d\.]", "");
            if (decimal.TryParse(cleaned, out var num))
                return num;
        }

        return value.Trim();
    }

    private object? GetDefaultValue(string fieldName)
    {
        var lower = fieldName.ToLower();
        
        return lower switch
        {
            var f when f.Contains("price") || f.Contains("cost") || f.Contains("amount") => 0m,
            var f when f.Contains("rating") || f.Contains("score") => 0.0,
            var f when f.Contains("quantity") || f.Contains("stock") || f.Contains("count") => 0,
            var f when f.Contains("name") || f.Contains("title") => "Unknown",
            var f when f.Contains("url") || f.Contains("link") => "",
            var f when f.Contains("description") || f.Contains("desc") => "",
            var f when f.Contains("image") || f.Contains("img") => "",
            _ => null
        };
    }

    private bool IsValidRecord(Dictionary<string, object?> record)
    {
        // A record is valid if it has at least one non-null, non-empty value
        return record.Any(kvp => kvp.Value != null && 
                                 kvp.Value.ToString() != string.Empty &&
                                 kvp.Value.ToString() != "0");
    }

    private string DetectFieldType(object? value)
    {
        if (value == null) return "null";

        return value switch
        {
            string => "string",
            int or long => "integer",
            float or double or decimal => "number",
            bool => "boolean",
            Array => "array",
            _ => "object"
        };
    }

    private void DetectSchemaTypeAndKeyFields(DataSchema schema)
    {
        var fields = schema.Fields.Select(f => f.ToLower()).ToList();

        // Detect schema type based on field patterns
        if (fields.Any(f => f.Contains("product") || f.Contains("price")))
        {
            schema.Type = "product_list";
            schema.LabelField = FindField(fields, new[] { "name", "product_name", "title", "productname" });
            schema.ValueField = FindField(fields, new[] { "price", "cost", "amount" });
        }
        else if (fields.Any(f => f.Contains("article") || f.Contains("post")))
        {
            schema.Type = "article_list";
            schema.LabelField = FindField(fields, new[] { "title", "name", "headline" });
            schema.ValueField = FindField(fields, new[] { "views", "likes", "rating" });
        }
        else if (fields.Any(f => f.Contains("user") || f.Contains("person")))
        {
            schema.Type = "user_list";
            schema.LabelField = FindField(fields, new[] { "name", "username", "fullname" });
            schema.ValueField = FindField(fields, new[] { "score", "points", "rating" });
        }
        else
        {
            schema.Type = "generic";
            // Try to find reasonable defaults
            schema.LabelField = FindField(fields, new[] { "name", "title", "label", "text" }) 
                ?? schema.Fields.FirstOrDefault();
            schema.ValueField = FindField(fields, new[] { "value", "count", "amount", "number" });
        }
    }

    private string? FindField(List<string> fields, string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var found = fields.FirstOrDefault(f => f == candidate || f.Contains(candidate));
            if (found != null)
                return found;
        }
        return null;
    }
}
