using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Chat.Commands;

/// <summary>
/// Command to upload a CSV file to a conversation
/// </summary>
public class UploadConversationCsvCommand : IRequest<UploadConversationCsvResponse>
{
    /// <summary>
    /// Conversation ID (from route parameter)
    /// </summary>
    [JsonIgnore]
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// The CSV file to upload
    /// </summary>
    [JsonIgnore]
    public IFormFile File { get; set; } = null!;
}

/// <summary>
/// Response for conversation CSV upload
/// </summary>
public class UploadConversationCsvResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public string? FileName { get; set; }
    public int? RowCount { get; set; }
    public List<string>? ColumnNames { get; set; }
}
