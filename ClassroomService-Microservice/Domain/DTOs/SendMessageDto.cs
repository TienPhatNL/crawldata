using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Domain.DTOs;

public class SendMessageDto
{
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    public Guid ReceiverId { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Link message to a specific support request for isolation
    /// </summary>
    public Guid? SupportRequestId { get; set; }
}
