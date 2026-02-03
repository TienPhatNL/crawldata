using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentsQuery : IRequest<GetAssignmentsResponse>
{
    // User context
    public Guid? RequestUserId { get; set; }
    public string? RequestUserRole { get; set; }
    
    // Required filter
    public Guid CourseId { get; set; }
    
    // Optional filters
    public List<AssignmentStatus>? Statuses { get; set; }
    public bool? IsGroupAssignment { get; set; }
    public Guid? AssignedToGroupId { get; set; }
    public bool? HasAssignedGroups { get; set; }
    
    // Date filters
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsUpcoming { get; set; } // Due within next 7 days
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
