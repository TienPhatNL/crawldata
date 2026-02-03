using MediatR;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class CreateAssignmentCommand : IRequest<CreateAssignmentResponse>
{
    public Guid CourseId { get; set; }
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// When assignment becomes available (optional, null means immediately available)
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    public DateTime DueDate { get; set; }
    public string Format { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this is a group or individual assignment
    /// </summary>
    public bool IsGroupAssignment { get; set; } = false;
    
    /// <summary>
    /// Maximum points/grade for this assignment
    /// </summary>
    public int? MaxPoints { get; set; }
}