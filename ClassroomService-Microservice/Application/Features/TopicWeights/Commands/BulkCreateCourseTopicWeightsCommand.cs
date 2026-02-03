using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;

namespace ClassroomService.Application.Features.TopicWeights.Commands;

/// <summary>
/// Command to bulk create TopicWeights for a specific course
/// Only allowed for Staff. Bypasses term validation if course status is PendingApproval.
/// </summary>
public class BulkCreateCourseTopicWeightsCommand : IRequest<BulkCreateCourseTopicWeightsResponse>
{
    /// <summary>
    /// Target course ID (enforced from URL)
    /// </summary>
    public Guid CourseId { get; set; }
    
    /// <summary>
    /// List of weights to create
    /// </summary>
    public List<TopicWeightCreateItem> Weights { get; set; } = new();
    
    /// <summary>
    /// Staff member configuring these weights
    /// </summary>
    public Guid ConfiguredBy { get; set; }
    
    /// <summary>
    /// Reason for this bulk configuration
    /// </summary>
    public string? ChangeReason { get; set; }
}
