namespace ClassroomService.Domain.DTOs;

/// <summary>
/// Request to create student accounts via UserService
/// </summary>
public class CreateStudentAccountsRequest
{
    public Guid RequestedBy { get; set; }
    public List<StudentAccountRequest> Students { get; set; } = new();
    public bool SendEmailCredentials { get; set; } = true;
    public bool CreateAccountIfNotFound { get; set; } = true;
}

/// <summary>
/// Individual student account request
/// </summary>
public class StudentAccountRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Response from UserService for student account creation
/// </summary>
public class CreateStudentAccountsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int Failed { get; set; }
    public List<StudentCreationResult> Results { get; set; } = new();
}

/// <summary>
/// Result for individual student account creation
/// </summary>
public class StudentCreationResult
{
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? TemporaryPassword { get; set; }
}
