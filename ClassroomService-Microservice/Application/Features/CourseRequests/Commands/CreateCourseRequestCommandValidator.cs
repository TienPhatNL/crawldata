using FluentValidation;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseRequests.Commands;

public class CreateCourseRequestCommandValidator : AbstractValidator<CreateCourseRequestCommand>
{
    public CreateCourseRequestCommandValidator()
    {
        RuleFor(x => x.CourseCodeId)
            .NotEmpty().WithMessage("Course code ID is required");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Course description is required")
            .Length(ValidationConstants.MinCourseDescriptionLength, ValidationConstants.MaxCourseDescriptionLength)
            .WithMessage($"Course description must be between {ValidationConstants.MinCourseDescriptionLength} and {ValidationConstants.MaxCourseDescriptionLength} characters");

        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage("Term ID is required");

        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("Lecturer ID is required");

        // Request reason validation (optional)
        When(x => !string.IsNullOrEmpty(x.RequestReason), () =>
        {
            RuleFor(x => x.RequestReason)
                .MaximumLength(500)
                .WithMessage("Request reason cannot exceed 500 characters");
        });
    }
}