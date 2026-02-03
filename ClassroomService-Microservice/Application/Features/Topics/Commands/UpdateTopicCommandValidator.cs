using FluentValidation;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Validator for UpdateTopicCommand
/// </summary>
public class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Topic ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Topic name is required")
            .MaximumLength(100).WithMessage("Topic name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}
