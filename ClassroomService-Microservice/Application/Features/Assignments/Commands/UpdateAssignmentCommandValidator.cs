using FluentValidation;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.DueDate)
            .When(x => x.StartDate.HasValue)
            .WithMessage("Start date must be before due date");

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .WithMessage("Due date is required");

        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("Format is required")
            .MaximumLength(100)
            .WithMessage("Format cannot exceed 100 characters");

        RuleFor(x => x.MaxPoints)
            .InclusiveBetween(1, 100)
            .When(x => x.MaxPoints.HasValue)
            .WithMessage("Max points must be between 1 and 100");
    }
}
