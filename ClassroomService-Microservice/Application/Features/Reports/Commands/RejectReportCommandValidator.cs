using FluentValidation;

namespace ClassroomService.Application.Features.Reports.Commands;

public class RejectReportCommandValidator : AbstractValidator<RejectReportCommand>
{
    public RejectReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty()
            .WithMessage("Report ID is required");

        RuleFor(x => x.Feedback)
            .NotEmpty()
            .WithMessage("Feedback is required when rejecting a report")
            .MaximumLength(2000)
            .WithMessage("Feedback must not exceed 2000 characters");
    }
}
