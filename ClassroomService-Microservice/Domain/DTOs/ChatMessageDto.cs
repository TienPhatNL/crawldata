namespace ClassroomService.Domain.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
