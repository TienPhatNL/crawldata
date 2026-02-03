using FluentValidation;

namespace UserService.Application.Features.Authentication.Commands;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.GoogleIdToken)
            .NotEmpty().WithMessage("Google ID token is required")
            .MinimumLength(10).WithMessage("Invalid Google ID token format");
    }
}
