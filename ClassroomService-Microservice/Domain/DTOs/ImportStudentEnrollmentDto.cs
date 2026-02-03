namespace ClassroomService.Domain.DTOs;

/// <summary>
/// DTO for importing student enrollments from Excel
/// </summary>
public class ImportStudentEnrollmentDto
{
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty; // Student ID for validation
    public string FirstName { get; set; } = string.Empty; // For account creation
    public string LastName { get; set; } = string.Empty; // For account creation
    public string? ProfilePictureUrl { get; set; } // Optional profile picture
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty; // Full course name (format: CourseCode#UniqueCode - Lecturer)
    public string Term { get; set; } = string.Empty;
    public int RowNumber { get; set; } // For error reporting
}