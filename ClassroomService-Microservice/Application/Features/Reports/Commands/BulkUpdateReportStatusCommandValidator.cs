using FluentValidation;

namespace ClassroomService.Application.Features.Reports.Commands;

public class BulkUpdateReportStatusCommandValidator : AbstractValidator<BulkUpdateReportStatusCommand>
{
    public BulkUpdateReportStatusCommandValidator()
    {
        RuleFor(x => x.ReportIds)
            .NotEmpty().WithMessage("At least one report ID is required")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot update more than 100 reports at once");
    }
}
