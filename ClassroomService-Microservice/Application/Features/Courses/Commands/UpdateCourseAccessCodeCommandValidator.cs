using FluentValidation;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Courses.Commands;

public class UpdateCourseAccessCodeCommandValidator : AbstractValidator<UpdateCourseAccessCodeCommand>
{
    public UpdateCourseAccessCodeCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course ID is required.");

        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("Lecturer ID is required.");

        // Access code validation rules
        When(x => x.RequiresAccessCode, () =>
        {
            When(x => x.AccessCodeType == AccessCodeType.Custom, () =>
            {
                RuleFor(x => x.CustomAccessCode)
                    .NotEmpty().WithMessage("Custom access code is required when AccessCodeType is Custom.")
                    .MinimumLength(3).WithMessage("Custom access code must be at least 3 characters.")
                    .MaximumLength(50).WithMessage("Custom access code must not exceed 50 characters.");
            });

            When(x => x.ExpiresAt.HasValue, () =>
            {
                RuleFor(x => x.ExpiresAt)
                    .GreaterThan(DateTime.UtcNow).WithMessage("Access code expiration date must be in the future.");
            });
        });
    }
}