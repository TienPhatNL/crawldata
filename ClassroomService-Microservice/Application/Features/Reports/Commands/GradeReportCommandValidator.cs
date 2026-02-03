using FluentValidation;

namespace ClassroomService.Application.Features.Reports.Commands;

public class GradeReportCommandValidator : AbstractValidator<GradeReportCommand>
{
    public GradeReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty().WithMessage("Report ID is required");

        RuleFor(x => x.Grade)
            .GreaterThanOrEqualTo(0).WithMessage("Grade must be greater than or equal to 0");

        RuleFor(x => x.Feedback)
            .MaximumLength(5000).WithMessage("Feedback must not exceed 5000 characters");
    }
}
