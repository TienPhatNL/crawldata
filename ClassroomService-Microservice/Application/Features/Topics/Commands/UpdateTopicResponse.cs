using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Response for update topic command
/// </summary>
public class UpdateTopicResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TopicDto? Topic { get; set; }
}
