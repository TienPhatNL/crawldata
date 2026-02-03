using MediatR;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to import students into a specific course from Excel file (Lecturers only)
/// </summary>
public class ImportCourseStudentsCommand : IRequest<ImportCourseStudentsResponse>
{
    /// <summary>
    /// The course ID to import students into
    /// </summary>
    [Required]
    public Guid CourseId { get; set; }

    /// <summary>
    /// Excel file with student data
    /// </summary>
    [Required]
    public IFormFile ExcelFile { get; set; } = null!;

    /// <summary>
    /// ID of the user performing the import (set by controller)
    /// </summary>
    public Guid ImportedBy { get; set; }
    
    /// <summary>
    /// Auto-create student accounts if not found (for allowed email domains)
    /// </summary>
    public bool CreateAccountIfNotFound { get; set; } = false;
}

/// <summary>
/// Response for importing students into a specific course
/// </summary>
public class ImportCourseStudentsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessfulEnrollments { get; set; }
    public int FailedEnrollments { get; set; }
    public int StudentsCreated { get; set; } // Number of students auto-created
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> CreatedStudentEmails { get; set; } = new List<string>(); // Emails of auto-created students
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
}