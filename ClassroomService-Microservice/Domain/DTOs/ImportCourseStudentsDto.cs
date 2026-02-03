namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for importing students into a specific course from Excel
/// </summary>
public class ImportCourseStudentsDto
{
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int RowNumber { get; set; } // For error reporting
}