namespace ClassroomService.Application.Features.TopicWeights.DTOs;

/// <summary>
/// Request DTO for creating a new TopicWeight in bulk operations
/// </summary>
public class TopicWeightCreateItem
{
    /// <summary>
    /// Topic ID for this weight configuration
    /// </summary>
    public Guid TopicId { get; set; }
    
    /// <summary>
    /// Weight percentage (0-100)
    /// </summary>
    public decimal WeightPercentage { get; set; }
    
    /// <summary>
    /// Optional description for this weight configuration
    /// </summary>
    public string? Description { get; set; }
}
