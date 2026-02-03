using FluentValidation;

namespace ClassroomService.Application.Features.Groups.Commands;

/// <summary>
/// Validator for CreateGroupCommand
/// </summary>
public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty()
            .WithMessage("Course ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Group name is required")
            .MaximumLength(100)
            .WithMessage("Group name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.MaxMembers)
            .GreaterThan(0)
            .When(x => x.MaxMembers.HasValue)
            .WithMessage("Max members must be greater than 0");
    }
}
