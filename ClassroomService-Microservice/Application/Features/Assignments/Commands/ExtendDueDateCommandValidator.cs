using FluentValidation;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class ExtendDueDateCommandValidator : AbstractValidator<ExtendDueDateCommand>
{
    public ExtendDueDateCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required");

        RuleFor(x => x.ExtendedDueDate)
            .NotEmpty()
            .WithMessage("Extended due date is required")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Extended due date must be in the future");
    }
}
