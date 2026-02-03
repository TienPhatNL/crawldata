using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

/// <summary>
/// Command to configure (create or update) multiple topic weights for a specific CourseCode
/// </summary>
public class BulkUpdateTopicWeightsCommand : IRequest<BulkUpdateTopicWeightsResponse>
{
    public Guid CourseCodeId { get; set; } // CourseCode to configure weights for
    public Guid ConfiguredBy { get; set; } // User performing the bulk configuration
    public string? ChangeReason { get; set; } // Optional reason for bulk configuration
    public List<TopicWeightConfigDto> Weights { get; set; } = new();
}

/// <summary>
/// Individual topic weight update item
/// </summary>
public class TopicWeightUpdateItem
{
    public Guid Id { get; set; }
    public decimal WeightPercentage { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Response for bulk update operation
/// </summary>
public class BulkUpdateTopicWeightsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Warning { get; set; } // Warning message (e.g., total < 100%)
    public List<TopicWeightResponseDto> UpdatedWeights { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
