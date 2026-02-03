namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Response for delete topic command
/// </summary>
public class DeleteTopicResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
