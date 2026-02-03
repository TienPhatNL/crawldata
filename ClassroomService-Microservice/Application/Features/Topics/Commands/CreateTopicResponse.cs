using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Response for create topic command
/// </summary>
public class CreateTopicResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TopicDto? Topic { get; set; }
}
