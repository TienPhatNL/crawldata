using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.SuspendedById)
            .NotEmpty().WithMessage("Suspended by user ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required")
            .MaximumLength(500).WithMessage("Suspension reason cannot exceed 500 characters");

        RuleFor(x => x.SuspendUntil)
            .GreaterThan(DateTime.UtcNow).WithMessage("Suspension end date must be in the future")
            .When(x => x.SuspendUntil.HasValue);
    }
}