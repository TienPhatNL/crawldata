using FluentValidation;

namespace UserService.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        RuleFor(x => x.EffectiveDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Effective date cannot be in the past")
            .When(x => x.EffectiveDate.HasValue);
    }
}