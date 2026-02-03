using System.Text.Json;

namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Container for assignment attachments, stored as JSON in the database
/// </summary>
public class AssignmentAttachmentsMetadata
{
    /// <summary>
    /// List of file attachments
    /// </summary>
    public List<AttachmentMetadata> Files { get; set; } = new List<AttachmentMetadata>();
    
    /// <summary>
    /// Serializes the attachments metadata to JSON string
    /// </summary>
    /// <returns>JSON string representation</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
    
    /// <summary>
    /// Deserializes JSON string to AttachmentMetadata object
    /// </summary>
    /// <param name="json">JSON string</param>
    /// <returns>Deserialized object or empty metadata if null/invalid</returns>
    public static AssignmentAttachmentsMetadata FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AssignmentAttachmentsMetadata();
        }
        
        try
        {
            var result = JsonSerializer.Deserialize<AssignmentAttachmentsMetadata>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
            
            return result ?? new AssignmentAttachmentsMetadata();
        }
        catch
        {
            return new AssignmentAttachmentsMetadata();
        }
    }
    
    /// <summary>
    /// Adds a new attachment to the collection
    /// </summary>
    public void AddAttachment(AttachmentMetadata attachment)
    {
        Files.Add(attachment);
    }
    
    /// <summary>
    /// Removes an attachment by ID
    /// </summary>
    /// <param name="fileId">File ID to remove</param>
    /// <returns>True if removed, false if not found</returns>
    public bool RemoveAttachment(Guid fileId)
    {
        var file = Files.FirstOrDefault(f => f.Id == fileId);
        if (file != null)
        {
            Files.Remove(file);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Gets an attachment by ID
    /// </summary>
    public AttachmentMetadata? GetAttachment(Guid fileId)
    {
        return Files.FirstOrDefault(f => f.Id == fileId);
    }
}
