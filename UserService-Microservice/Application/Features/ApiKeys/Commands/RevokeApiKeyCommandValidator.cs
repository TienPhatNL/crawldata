using FluentValidation;

namespace UserService.Application.Features.ApiKeys.Commands;

public class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ApiKeyId)
            .NotEmpty().WithMessage("API key ID is required");
    }
}