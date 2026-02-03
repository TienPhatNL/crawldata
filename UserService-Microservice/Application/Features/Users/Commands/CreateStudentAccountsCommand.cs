using MediatR;
using UserService.Application.Common.Models;

namespace UserService.Application.Features.Users.Commands;

public class CreateStudentAccountsCommand : IRequest<ResponseModel>
{
    public Guid RequestedBy { get; set; } // Lecturer or Staff requesting
    public Guid? ClassroomId { get; set; } // Optional - for Lecturer requests
    public List<StudentAccountRequest> Students { get; set; } = new();
    public bool SendEmailCredentials { get; set; } = true;
    public bool CreateAccountIfNotFound { get; set; } = false;
    public string? Notes { get; set; }
}

public class StudentAccountRequest
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string StudentId { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}