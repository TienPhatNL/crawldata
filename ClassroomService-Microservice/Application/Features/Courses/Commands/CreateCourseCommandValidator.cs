using FluentValidation;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Commands;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
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

        // Access code validations
        When(x => x.RequiresAccessCode, () =>
        {
            When(x => x.AccessCodeType == AccessCodeType.Custom, () =>
            {
                RuleFor(x => x.CustomAccessCode)
                    .NotEmpty().WithMessage("Custom access code is required when access code type is Custom")
                    .Length(ValidationConstants.MinCustomCodeLength, ValidationConstants.MaxCustomCodeLength)
                    .WithMessage($"Custom access code must be between {ValidationConstants.MinCustomCodeLength} and {ValidationConstants.MaxCustomCodeLength} characters");
            });
        });
    }
}