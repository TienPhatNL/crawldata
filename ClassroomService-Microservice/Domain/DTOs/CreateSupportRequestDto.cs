using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.DTOs;

public class CreateSupportRequestDto
{
    public Guid CourseId { get; set; }
    public SupportPriority Priority { get; set; } = SupportPriority.Medium;
    public SupportRequestCategory Category { get; set; } = SupportRequestCategory.Technical;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
