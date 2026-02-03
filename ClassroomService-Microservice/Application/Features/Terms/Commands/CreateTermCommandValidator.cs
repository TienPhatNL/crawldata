using FluentValidation;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Terms.Commands;

public class CreateTermCommandValidator : AbstractValidator<CreateTermCommand>
{
    private readonly ITermRepository _termRepository;

    public CreateTermCommandValidator(ITermRepository termRepository)
    {
        _termRepository = termRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Term name is required")
            .MaximumLength(100).WithMessage("Term name must not exceed 100 characters")
            .MustAsync(BeUniqueName).WithMessage("A term with this name already exists");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required")
            .Must(BeValidDate).WithMessage("Start date must be a valid date");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .Must(BeValidDate).WithMessage("End date must be a valid date")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

        RuleFor(x => x)
            .MustAsync(NotOverlapWithExistingTerms)
            .WithMessage("The term dates overlap with an existing term")
            .WithName("Term dates");
    }

    private bool BeValidDate(DateTime date)
    {
        return date != default && date.Year >= 1900 && date.Year <= 2100;
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var existingTerm = await _termRepository.GetTermByNameAsync(name, cancellationToken);
        return existingTerm == null;
    }

    private async Task<bool> NotOverlapWithExistingTerms(CreateTermCommand command, CancellationToken cancellationToken)
    {
        var overlappingTerm = await _termRepository.GetOverlappingTermAsync(
            command.StartDate,
            command.EndDate,
            null, // No ID to exclude for create
            cancellationToken);

        return overlappingTerm == null;
    }
}
