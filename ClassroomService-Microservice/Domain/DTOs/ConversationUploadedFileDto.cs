namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for uploaded files in a conversation
/// </summary>
public class ConversationUploadedFileDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int RowCount { get; set; }
    public List<string> ColumnNames { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public Guid UploadedBy { get; set; }
}
