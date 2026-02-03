using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

/// <summary>
/// Query to get all assignments belonging to a specific topic with filtering and pagination
/// </summary>
public class GetAssignmentsByTopicQuery : IRequest<GetAssignmentsByTopicResponse>
{
    // User context
    public Guid? RequestUserId { get; set; }
    public string? RequestUserRole { get; set; }
    
    // Required filter
    public Guid TopicId { get; set; }
    
    // Optional filters
    public Guid? CourseId { get; set; }
    public List<AssignmentStatus>? Statuses { get; set; }
    public bool? IsGroupAssignment { get; set; }
    
    // Date filters
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsUpcoming { get; set; }
    public bool? IsOverdue { get; set; }
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Sorting
    public string SortBy { get; set; } = "DueDate";
    public string SortOrder { get; set; } = "asc";
    
    // Search
    public string? SearchQuery { get; set; }
}
