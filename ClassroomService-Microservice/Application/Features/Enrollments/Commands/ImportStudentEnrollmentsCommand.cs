using MediatR;
using Microsoft.AspNetCore.Http;

namespace ClassroomService.Application.Features.Enrollments.Commands;

/// <summary>
/// Command to import student enrollments from Excel file (Lecturers and Staff only)
/// </summary>
public class ImportStudentEnrollmentsCommand : IRequest<ImportStudentEnrollmentsResponse>
{
    public IFormFile ExcelFile { get; set; } = null!;
    public Guid ImportedBy { get; set; } // Lecturer or Staff member performing the import
    public bool CreateAccountIfNotFound { get; set; } = false; // Auto-create student accounts for allowed email domains
}

public class ImportStudentEnrollmentsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessfulEnrollments { get; set; }
    public int FailedEnrollments { get; set; }
    public int StudentsCreated { get; set; } // Number of students auto-created
    public List<string> Errors { get; set; } = new();
    public List<Guid> EnrolledCourseIds { get; set; } = new();
    public List<string> CreatedStudentEmails { get; set; } = new(); // Emails of auto-created students
}