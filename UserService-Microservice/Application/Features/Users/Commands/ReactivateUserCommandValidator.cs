using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ReactivatedById)
            .NotEmpty().WithMessage("Reactivated by user ID is required");
    }
}