using FluentValidation;

namespace UserService.Application.Features.ApiKeys.Queries;

public class GetApiKeysQueryValidator : AbstractValidator<GetApiKeysQuery>
{
    public GetApiKeysQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}