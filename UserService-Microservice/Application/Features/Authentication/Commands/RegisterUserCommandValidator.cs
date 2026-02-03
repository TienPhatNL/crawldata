using FluentValidation;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Authentication.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number and one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role")
            .Must(role => role != UserRole.Admin && role != UserRole.Staff)
            .WithMessage("Cannot register directly as Admin or Staff");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // Lecturer-specific validations
        When(x => x.Role == UserRole.Lecturer, () =>
        {
            RuleFor(x => x.InstitutionName)
                .NotEmpty().WithMessage("Institution name is required for lecturers")
                .MaximumLength(255).WithMessage("Institution name must not exceed 255 characters");

            RuleFor(x => x.InstitutionEmail)
                .NotEmpty().WithMessage("Institution email is required for lecturers")
                .EmailAddress().WithMessage("Invalid institution email format");

            RuleFor(x => x.Department)
                .NotEmpty().WithMessage("Department is required for lecturers")
                .MaximumLength(100).WithMessage("Department must not exceed 100 characters");

            RuleFor(x => x.Position)
                .NotEmpty().WithMessage("Position is required for lecturers")
                .MaximumLength(100).WithMessage("Position must not exceed 100 characters");
        });

        // Student-specific validations (when created by staff/lecturer)
        When(x => x.Role == UserRole.Student, () =>
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required for students")
                .MaximumLength(50).WithMessage("Student ID must not exceed 50 characters");
        });
    }
}