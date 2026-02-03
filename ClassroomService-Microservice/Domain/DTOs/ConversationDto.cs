namespace ClassroomService.Domain.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    
    // Other person in conversation
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserRole { get; set; } = string.Empty;
    
    // Last message
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    
    // Unread message count
    public int UnreadCount { get; set; } = 0;
}
