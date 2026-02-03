using System.Text;
using System.Text.Json;

namespace ClassroomService.Application.Common.Helpers;

/// <summary>
/// Helper class for parsing CSV files
/// </summary>
public static class CsvParserHelper
{
    private static readonly char[] CandidateDelimiters = { ',', ';', '\t', '|' };

    /// <summary>
    /// Parses a CSV file stream and returns structured data
    /// </summary>
    /// <param name="stream">CSV file stream</param>
    /// <param name="encoding">File encoding (default: UTF-8)</param>
    /// <returns>Parsed CSV data with columns, rows, and row count</returns>
    public static CsvParseResult ParseCsv(Stream stream, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var reader = new StreamReader(stream, encoding, leaveOpen: true);
        var lines = new List<string>();
        string? line;
        
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("CSV file is empty");
        }

        // Detect delimiter based on sample lines (supports comma/semicolon/tab/pipe)
        var delimiter = DetectDelimiter(lines);

        // Parse header row
        var headerLine = lines[0];
        var columns = ParseCsvLine(headerLine, delimiter);
        
        if (columns.Length == 0)
        {
            throw new InvalidOperationException("CSV file has no columns");
        }

        // Parse data rows
        var rows = new List<Dictionary<string, string>>();
        
        for (int i = 1; i < lines.Count; i++)
        {
            var values = ParseCsvLine(lines[i], delimiter);
            
            // Skip empty rows
            if (values.All(v => string.IsNullOrWhiteSpace(v)))
            {
                continue;
            }

            // If row has fewer values than columns, pad with empty strings
            // If row has more values than columns, truncate
            var rowDict = new Dictionary<string, string>();
            for (int j = 0; j < columns.Length; j++)
            {
                var columnName = columns[j].Trim();
                var value = j < values.Length ? values[j].Trim() : string.Empty;
                rowDict[columnName] = value;
            }
            
            rows.Add(rowDict);
        }

        return new CsvParseResult
        {
            Columns = columns,
            Rows = rows,
            RowCount = rows.Count
        };
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted values and escaped quotes
    /// </summary>
    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool insideQuotes = false;
        bool skipNextChar = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (skipNextChar)
            {
                skipNextChar = false;
                continue;
            }

            char currentChar = line[i];
            char? nextChar = i + 1 < line.Length ? line[i + 1] : null;

            if (currentChar == '"')
            {
                if (insideQuotes && nextChar == '"')
                {
                    // Escaped quote inside quoted value
                    currentValue.Append('"');
                    skipNextChar = true;
                }
                else
                {
                    // Toggle quote state
                    insideQuotes = !insideQuotes;
                }
            }
            else if (currentChar == delimiter && !insideQuotes)
            {
                // End of value
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(currentChar);
            }
        }

        // Add the last value
        values.Add(currentValue.ToString());

        return values.ToArray();
    }

    private static char DetectDelimiter(IEnumerable<string> lines)
    {
        var sampleLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Take(5).ToList();
        if (sampleLines.Count == 0)
        {
            return ',';
        }

        var bestDelimiter = ',';
        var bestScore = -1;

        foreach (var delimiter in CandidateDelimiters)
        {
            var score = 0;
            foreach (var line in sampleLines)
            {
                score += CountDelimiterOutsideQuotes(line, delimiter);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDelimiter = delimiter;
            }
        }

        return bestScore > 0 ? bestDelimiter : ',';
    }

    private static int CountDelimiterOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        bool insideQuotes = false;
        bool skipNextChar = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (skipNextChar)
            {
                skipNextChar = false;
                continue;
            }

            char currentChar = line[i];
            char? nextChar = i + 1 < line.Length ? line[i + 1] : null;

            if (currentChar == '"')
            {
                if (insideQuotes && nextChar == '"')
                {
                    skipNextChar = true;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (currentChar == delimiter && !insideQuotes)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Converts CSV parse result to JSON strings
    /// </summary>
    public static (string DataJson, string ColumnNamesJson) ToJson(CsvParseResult result)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var dataJson = JsonSerializer.Serialize(result.Rows, options);
        var columnNamesJson = JsonSerializer.Serialize(result.Columns, options);

        return (dataJson, columnNamesJson);
    }
}

/// <summary>
/// Result of CSV parsing operation
/// </summary>
public class CsvParseResult
{
    /// <summary>
    /// Column names from CSV header
    /// </summary>
    public string[] Columns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Data rows as dictionaries (column name -> value)
    /// </summary>
    public List<Dictionary<string, string>> Rows { get; set; } = new();

    /// <summary>
    /// Number of data rows (excluding header)
    /// </summary>
    public int RowCount { get; set; }
}
