using FluentValidation;
using ClassroomService.Domain.Constants;

namespace ClassroomService.Application.Features.CourseCodes.Commands;

public class CreateCourseCodeCommandValidator : AbstractValidator<CreateCourseCodeCommand>
{
    public CreateCourseCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Course code is required")
            .Length(ValidationConstants.MinCourseCodeLength, ValidationConstants.MaxCourseCodeLength)
            .WithMessage($"Course code must be between {ValidationConstants.MinCourseCodeLength} and {ValidationConstants.MaxCourseCodeLength} characters")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Course code must contain only uppercase letters and numbers");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Course title is required")
            .Length(ValidationConstants.MinCourseCodeTitleLength, ValidationConstants.MaxCourseCodeTitleLength)
            .WithMessage($"Course title must be between {ValidationConstants.MinCourseCodeTitleLength} and {ValidationConstants.MaxCourseCodeTitleLength} characters");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required")
            .Length(ValidationConstants.MinDepartmentLength, ValidationConstants.MaxDepartmentLength)
            .WithMessage($"Department must be between {ValidationConstants.MinDepartmentLength} and {ValidationConstants.MaxDepartmentLength} characters");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.MaxCourseCodeDescriptionLength)
            .WithMessage($"Description cannot exceed {ValidationConstants.MaxCourseCodeDescriptionLength} characters");
    }
}