using MediatR;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetMyAssignmentsQuery : IRequest<GetMyAssignmentsResponse>
{
    public Guid StudentId { get; set; }
    public string? RequestUserRole { get; set; }
    
    // Optional filters
    public Guid? CourseId { get; set; }
    public List<AssignmentStatus>? Statuses { get; set; }
    public bool? IsUpcoming { get; set; }
    public bool? IsOverdue { get; set; }
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string SortBy { get; set; } = "DueDate";
    public string SortOrder { get; set; } = "asc";
}
