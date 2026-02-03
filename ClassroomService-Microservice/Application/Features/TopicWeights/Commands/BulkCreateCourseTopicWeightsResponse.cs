using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

/// <summary>
/// Response for bulk create course-specific TopicWeights operation
/// </summary>
public class BulkCreateCourseTopicWeightsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TopicWeightResponseDto> CreatedWeights { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? Warning { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
