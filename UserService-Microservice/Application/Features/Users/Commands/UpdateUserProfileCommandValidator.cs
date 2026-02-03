using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.InstitutionName)
            .MaximumLength(200).WithMessage("Institution name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.InstitutionName));

        RuleFor(x => x.InstitutionAddress)
            .MaximumLength(500).WithMessage("Institution address cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.InstitutionAddress));

        RuleFor(x => x.StudentId)
            .MaximumLength(50).WithMessage("Student ID cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.StudentId));

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Department));
    }
}