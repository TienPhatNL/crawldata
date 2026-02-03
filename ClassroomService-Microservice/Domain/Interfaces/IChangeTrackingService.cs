namespace ClassroomService.Domain.Interfaces;

/// <summary>
/// Service for calculating and tracking text differences between content versions
/// </summary>
public interface IChangeTrackingService
{
    /// <summary>
    /// Calculate detailed diff between two text versions
    /// </summary>
    /// <param name="oldContent">Original content</param>
    /// <param name="newContent">Updated content</param>
    /// <returns>Structured diff result with operations</returns>
    DiffResult CalculateDiff(string? oldContent, string? newContent);
    
    /// <summary>
    /// Generate human-readable summary of changes
    /// </summary>
    /// <param name="diff">Diff result to summarize</param>
    /// <returns>Summary text like "Modified 3 lines, added 2 paragraphs, deleted 1 sentence"</returns>
    string GenerateSummary(DiffResult diff);
    
    /// <summary>
    /// Create unified diff format (like git diff)
    /// </summary>
    /// <param name="oldContent">Original content</param>
    /// <param name="newContent">Updated content</param>
    /// <returns>Unified diff string with +/- prefixes</returns>
    string CreateUnifiedDiff(string? oldContent, string? newContent);
    
    /// <summary>
    /// Convert diff result to JSON array of change operations
    /// </summary>
    /// <param name="diff">Diff result to serialize</param>
    /// <returns>JSON string of change operations</returns>
    string SerializeChangeOperations(DiffResult diff);
}

/// <summary>
/// Result of diff calculation containing all change operations and statistics
/// </summary>
public class DiffResult
{
    public List<ChangeOperation> Operations { get; set; } = new();
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public int LinesModified { get; set; }
    public int CharactersAdded { get; set; }
    public int CharactersDeleted { get; set; }
}

/// <summary>
/// Represents a single change operation (insert, delete, or replace)
/// </summary>
public class ChangeOperation
{
    public ChangeOperationType Type { get; set; }
    public int Position { get; set; }
    public int Length { get; set; }
    public string? OldText { get; set; }
    public string? NewText { get; set; }
    public int LineNumber { get; set; }
}

/// <summary>
/// Type of change operation
/// </summary>
public enum ChangeOperationType
{
    Insert = 1,
    Delete = 2,
    Replace = 3,
    Unchanged = 4
}
