using FluentValidation;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Subscriptions.Commands;

public class UpgradeSubscriptionCommandValidator : AbstractValidator<UpgradeSubscriptionCommand>
{
    public UpgradeSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty().WithMessage("Subscription plan ID is required");

        RuleFor(x => x.CustomQuotaLimit)
            .GreaterThan(0).WithMessage("Custom quota limit must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Custom quota limit cannot exceed 10,000")
            .When(x => x.CustomQuotaLimit.HasValue);

        RuleFor(x => x.PaymentReference)
            .MaximumLength(100).WithMessage("Payment reference cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));
    }
}