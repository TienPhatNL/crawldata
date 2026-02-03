namespace ClassroomService.Application.Features.Assignments.DTOs;

/// <summary>
/// DTO containing grade information for a single assignment
/// </summary>
public class AssignmentGradeDto
{
    /// <summary>
    /// Assignment ID
    /// </summary>
    public Guid AssignmentId { get; set; }
    
    /// <summary>
    /// Assignment name/title
    /// </summary>
    public string AssignmentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Assignment due date
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Score received (from Report.Grade)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Maximum points for this assignment
    /// </summary>
    public int MaxPoints { get; set; }
    
    /// <summary>
    /// Percentage score (Score/MaxPoints * 100)
    /// </summary>
    public decimal Percentage { get; set; }
    
    /// <summary>
    /// Submission status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
