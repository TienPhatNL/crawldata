using ClassroomService.Application.Features.Assignments.DTOs;

namespace ClassroomService.Application.Features.Assignments.Queries;

/// <summary>
/// Response for get assignments by topic query
/// </summary>
public class GetAssignmentsByTopicResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AssignmentSummaryDto> Assignments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
