using FluentValidation;

namespace ClassroomService.Application.Features.Reports.Commands;

public class SubmitReportCommandValidator : AbstractValidator<SubmitReportCommand>
{
    public SubmitReportCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("Assignment ID is required");

        RuleFor(x => x.Submission)
            .NotEmpty().WithMessage("Submission content is required")
            .MaximumLength(50000).WithMessage("Submission content must not exceed 50000 characters");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required for group submissions")
            .When(x => x.IsGroupSubmission);
    }
}
