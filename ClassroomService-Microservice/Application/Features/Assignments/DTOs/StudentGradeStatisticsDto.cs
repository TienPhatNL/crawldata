namespace ClassroomService.Application.Features.Assignments.DTOs;

/// <summary>
/// DTO containing student grade statistics for a specific course
/// </summary>
public class StudentGradeStatisticsDto
{
    /// <summary>
    /// Number of graded assignments completed
    /// </summary>
    public int CompletedAssignmentsCount { get; set; }
    
    /// <summary>
    /// Average score across all graded assignments (percentage)
    /// </summary>
    public decimal AverageScore { get; set; }
    
    /// <summary>
    /// List of all graded assignments with scores
    /// </summary>
    public List<AssignmentGradeDto> AssignmentGrades { get; set; } = new List<AssignmentGradeDto>();
}
