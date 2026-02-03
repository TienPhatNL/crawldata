using System.Text.Json;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for report history records
/// </summary>
public class ReportHistoryDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public int Version { get; set; }
    
    /// <summary>
    /// Sequence number within the version (e.g., v2.1, v2.2, v2.3)
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Full version string in format "Version.Sequence" (e.g., "2.3")
    /// </summary>
    public string FullVersion { get; set; } = string.Empty;
    
    public string? Comment { get; set; }
    public Dictionary<string, object>? Changes { get; set; }
    
    /// <summary>
    /// Human-readable summary of changes (e.g., "+2 lines, -1 lines | +150 chars, -50 chars")
    /// </summary>
    public string? ChangeSummary { get; set; }
    
    /// <summary>
    /// Detailed JSON of change operations for programmatic processing
    /// </summary>
    public string? ChangeDetails { get; set; }
    
    /// <summary>
    /// Git-style unified diff format for visualization
    /// </summary>
    public string? UnifiedDiff { get; set; }
    
    /// <summary>
    /// List of contributor IDs who made changes in this version
    /// </summary>
    public List<string> ContributorIds { get; set; } = new();
    
    /// <summary>
    /// List of contributor names who made changes in this version
    /// </summary>
    public List<string> ContributorNames { get; set; } = new();
}

/// <summary>
/// Response for getting report history with pagination
/// </summary>
public class ReportHistoryResponse
{
    public Guid ReportId { get; set; }
    public int CurrentVersion { get; set; }
    public List<ReportHistoryDto> History { get; set; } = new();
    
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of history records
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Whether there's a previous page
    /// </summary>
    public bool HasPrevious { get; set; }
    
    /// <summary>
    /// Whether there's a next page
    /// </summary>
    public bool HasNext { get; set; }
}

/// <summary>
/// DTO for version comparison
/// </summary>
public class ReportVersionDto
{
    public int Version { get; set; }
    
    /// <summary>
    /// Sequence number within the version (e.g., v2.1, v2.2, v2.3)
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Full version string in format "Version.Sequence" (e.g., "2.3")
    /// </summary>
    public string FullVersion { get; set; } = string.Empty;
    
    public string? Content { get; set; }
    public string? Status { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Action { get; set; }
}

/// <summary>
/// DTO for aggregated version information
/// Contains final state after all sequences in a version
/// </summary>
public class AggregatedVersionDto
{
    public int Version { get; set; }
    
    /// <summary>
    /// Full version range (e.g., "2.1-2.3" if version has sequences 1 through 3)
    /// </summary>
    public string FullVersionRange { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of sequences in this version
    /// </summary>
    public int SequenceCount { get; set; }
    
    /// <summary>
    /// Final content after all sequences
    /// </summary>
    public string? FinalContent { get; set; }
    
    /// <summary>
    /// Final status after all sequences
    /// </summary>
    public string? FinalStatus { get; set; }
    
    /// <summary>
    /// Final grade after all sequences (if graded)
    /// </summary>
    public decimal? FinalGrade { get; set; }
    
    /// <summary>
    /// Final feedback after all sequences (if graded)
    /// </summary>
    public string? FinalFeedback { get; set; }
    
    /// <summary>
    /// List of all actions performed in this version
    /// Example: ["Updated", "Submitted", "Graded"]
    /// </summary>
    public List<string> ActionsPerformed { get; set; } = new();
    
    /// <summary>
    /// List of all contributor IDs in this version
    /// </summary>
    public List<string> Contributors { get; set; } = new();
    
    /// <summary>
    /// List of all contributor names in this version
    /// </summary>
    public List<string> ContributorNames { get; set; } = new();
    
    /// <summary>
    /// Timestamp of first change in this version
    /// </summary>
    public DateTime FirstChangeAt { get; set; }
    
    /// <summary>
    /// Timestamp of last change in this version
    /// </summary>
    public DateTime LastChangeAt { get; set; }
}

/// <summary>
/// Response for comparing two versions
/// </summary>
public class CompareVersionsResponse
{
    public Guid ReportId { get; set; }
    
    /// <summary>
    /// Comparison mode used (Sequence, Version, or IntraVersion)
    /// </summary>
    public string Mode { get; set; } = "Sequence";
    
    // For Sequence mode (specific sequence comparison)
    public ReportVersionDto? Version1 { get; set; }
    public ReportVersionDto? Version2 { get; set; }
    
    // For Version mode (aggregate comparison)
    public AggregatedVersionDto? AggregatedVersion1 { get; set; }
    public AggregatedVersionDto? AggregatedVersion2 { get; set; }
    
    // For IntraVersion mode (evolution within same version)
    /// <summary>
    /// Timeline of sequences within the version (for IntraVersion mode)
    /// </summary>
    public List<ReportVersionDto>? SequenceTimeline { get; set; }
    
    /// <summary>
    /// Field-level differences between versions
    /// </summary>
    public List<FieldDifferenceDto> Differences { get; set; } = new();
    
    /// <summary>
    /// Git-style unified diff between versions
    /// </summary>
    public string? UnifiedDiff { get; set; }
    
    /// <summary>
    /// Human-readable summary of changes between versions
    /// </summary>
    public string? ChangeSummary { get; set; }
    
    /// <summary>
    /// Contributors who made changes between these versions
    /// </summary>
    public List<string> ContributorNames { get; set; } = new();
}

/// <summary>
/// DTO for field differences between versions
/// </summary>
public class FieldDifferenceDto
{
    public string Field { get; set; } = string.Empty;
    public bool Changed { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// DTO for timeline item
/// </summary>
public class TimelineItemDto
{
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    
    /// <summary>
    /// List of all contributor IDs who participated in this action
    /// </summary>
    public List<string> ContributorIds { get; set; } = new();
    
    /// <summary>
    /// List of all contributor names who participated in this action
    /// Extracted from Comment field (e.g., "Action description | Contributors: John Doe, Jane Smith")
    /// </summary>
    public List<string> ContributorNames { get; set; } = new();
    
    public string Action { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Response for timeline
/// </summary>
public class TimelineResponse
{
    public Guid ReportId { get; set; }
    public List<TimelineItemDto> Timeline { get; set; } = new();
}
