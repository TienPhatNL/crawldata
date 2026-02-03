using FluentValidation;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Application.Features.Reports.Commands;

public class UpdateReportStatusCommandValidator : AbstractValidator<UpdateReportStatusCommand>
{
    public UpdateReportStatusCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty()
            .WithMessage("Report ID is required");

        RuleFor(x => x.TargetStatus)
            .IsInEnum()
            .WithMessage("Invalid target status")
            .Must(status => status == ReportStatus.Draft || 
                           status == ReportStatus.RequiresRevision)
            .WithMessage("Only Draft or RequiresRevision status are allowed");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Comment))
            .WithMessage("Comment must not exceed 1000 characters");
    }
}
