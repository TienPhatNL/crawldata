namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for a single change made to a report
/// </summary>
public class ReportChangeDto
{
    public Guid ReportId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CursorPosition { get; set; }
    public DateTime Timestamp { get; set; }
    public string ChangeType { get; set; } = "content"; // "content", "cursor", "typing"
}

/// <summary>
/// DTO for collaborator presence information
/// </summary>
public class CollaboratorPresenceDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int CursorPosition { get; set; }
    public bool IsTyping { get; set; }
    public DateTime LastActivity { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for the full collaboration session state
/// </summary>
public class ReportCollaborationSessionDto
{
    public Guid ReportId { get; set; }
    public List<CollaboratorPresenceDto> ActiveUsers { get; set; } = new();
    public int PendingChangesCount { get; set; }
    public DateTime? LastChangeAt { get; set; }
    public DateTime SessionStartedAt { get; set; }
    public bool HasUnsavedChanges { get; set; }
    public int CurrentVersion { get; set; }
}

/// <summary>
/// Response for manual save operation
/// </summary>
public class ManualSaveResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int NewVersion { get; set; }
    public DateTime SavedAt { get; set; }
    public List<string> Contributors { get; set; } = new();
}

/// <summary>
/// DTO for version created notification
/// </summary>
public class VersionCreatedNotification
{
    public Guid ReportId { get; set; }
    public int Version { get; set; }
    public List<string> Contributors { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool IsAutoSave { get; set; }
}



