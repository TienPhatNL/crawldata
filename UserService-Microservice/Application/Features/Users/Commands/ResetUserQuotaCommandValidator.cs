using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class ResetUserQuotaCommandValidator : AbstractValidator<ResetUserQuotaCommand>
{
    public ResetUserQuotaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}