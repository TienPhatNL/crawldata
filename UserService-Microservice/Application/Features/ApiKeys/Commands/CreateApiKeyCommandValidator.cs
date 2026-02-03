using FluentValidation;
using UserService.Domain.Enums;

namespace UserService.Application.Features.ApiKeys.Commands;

public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required")
            .MaximumLength(100).WithMessage("API key name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .LessThan(DateTime.UtcNow.AddYears(2)).WithMessage("Expiration date cannot be more than 2 years from now")
            .When(x => x.ExpiresAt.HasValue);

        RuleFor(x => x.Scopes)
            .Must(scopes => scopes == null || scopes.Count <= 10)
            .WithMessage("Cannot have more than 10 scopes")
            .Must(scopes => scopes == null || scopes.All(s => Enum.TryParse<ApiKeyScope>(s, true, out _)))
            .WithMessage("One or more scopes are invalid")
            .When(x => x.Scopes != null);
    }
}