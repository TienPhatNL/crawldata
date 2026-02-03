namespace ClassroomService.Application.Features.Topics.DTOs;

/// <summary>
/// Minimal topic DTO for dropdown selection
/// </summary>
public class TopicDropdownDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
