using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Response for get topic by ID query
/// </summary>
public class GetTopicByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TopicDto? Topic { get; set; }
}
