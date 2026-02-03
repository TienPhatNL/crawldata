using FluentValidation;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class SelfUnenrollCommandValidator : AbstractValidator<SelfUnenrollCommand>
{
    public SelfUnenrollCommandValidator()
    {
        RuleFor(v => v.CourseId)
            .NotEmpty().WithMessage("Course ID is required.")
            .NotEqual(Guid.Empty).WithMessage("Course ID cannot be empty.");

        RuleFor(v => v.StudentId)
            .NotEmpty().WithMessage("Student ID is required.")
            .NotEqual(Guid.Empty).WithMessage("Student ID cannot be empty.");
    }
}