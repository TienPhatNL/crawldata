using ClassroomService.Domain.DTOs;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? ExtendedDueDate { get; set; }
    public string Format { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// List of file attachments (instructions, reference materials, etc.)
    /// </summary>
    public List<AttachmentMetadata>? Attachments { get; set; }
}