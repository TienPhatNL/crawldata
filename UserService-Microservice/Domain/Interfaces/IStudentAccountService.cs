namespace UserService.Domain.Interfaces;

/// <summary>
/// Service for creating and managing student accounts
/// Used by Kafka consumers to delegate student creation logic
/// </summary>
public interface IStudentAccountService
{
    /// <summary>
    /// Creates student accounts in bulk
    /// </summary>
    /// <param name="request">Student creation request with list of students</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing created students and any errors</returns>
    Task<StudentAccountCreationResult> CreateStudentAccountsAsync(
        CreateStudentAccountsRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for creating student accounts
/// </summary>
public class CreateStudentAccountsRequest
{
    public Guid RequestedBy { get; set; }
    public List<StudentAccountInfo> Students { get; set; } = new();
    public bool SendEmailCredentials { get; set; } = true;
    public bool CreateAccountIfNotFound { get; set; } = true;
}

/// <summary>
/// Student information for account creation
/// </summary>
public class StudentAccountInfo
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string StudentId { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Result of student account creation
/// </summary>
public class StudentAccountCreationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int Failed { get; set; }
    public List<StudentAccountResult> Results { get; set; } = new();
}

/// <summary>
/// Individual student account creation result
/// </summary>
public class StudentAccountResult
{
    public string Email { get; set; } = null!;
    public string StudentId { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? TemporaryPassword { get; set; }
}
