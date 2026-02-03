namespace ClassroomService.Application.Features.Assignments.Queries;

public class GetAssignmentStatisticsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    
    // Overall statistics
    public int TotalAssignments { get; set; }
    
    // By status
    public Dictionary<string, int> ByStatus { get; set; } = new();
    
    // By type
    public int IndividualAssignments { get; set; }
    public int GroupAssignments { get; set; }
    
    // Time-based
    public int UpcomingAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    
    // Group statistics
    public int TotalGroupsAssigned { get; set; }
    public int AssignmentsWithGroups { get; set; }
    public int AssignmentsWithoutGroups { get; set; }
    
    // Date statistics
    public DateTime? EarliestDueDate { get; set; }
    public DateTime? LatestDueDate { get; set; }
    public double? AverageDaysUntilDue { get; set; }
}
