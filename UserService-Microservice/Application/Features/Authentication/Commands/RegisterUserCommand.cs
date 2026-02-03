using MediatR;
using UserService.Application.Common.Models;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Authentication.Commands;

public class RegisterUserCommand : IRequest<ResponseModel>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Student;
    public string? PhoneNumber { get; set; }

    // For Lecturers
    public string? InstitutionName { get; set; }
    public string? InstitutionEmail { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }

    // For Students
    public string? StudentId { get; set; }
}