using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class UpdateUserQuotaCommandValidator : AbstractValidator<UpdateUserQuotaCommand>
{
    public UpdateUserQuotaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.NewQuotaLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Quota limit must be greater than or equal to 0")
            .LessThanOrEqualTo(10000).WithMessage("Quota limit cannot exceed 10,000");
    }
}