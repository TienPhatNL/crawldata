using FluentValidation;

namespace ClassroomService.Application.Features.Courses.Commands;

public class InactivateCourseCommandValidator : AbstractValidator<InactivateCourseCommand>
{
    public InactivateCourseCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course ID is required");

        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("Lecturer ID is required");

        When(x => !string.IsNullOrEmpty(x.Reason), () =>
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason cannot exceed 500 characters");
        });
    }
}
