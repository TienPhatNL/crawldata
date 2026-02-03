using FluentValidation;
using ClassroomService.Application.Features.Terms.Commands;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Terms.Commands;

public class UpdateTermCommandValidator : AbstractValidator<UpdateTermCommand>
{
    private readonly ITermRepository _termRepository;

    public UpdateTermCommandValidator(ITermRepository termRepository)
    {
        _termRepository = termRepository;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Term ID is required");

        When(x => !string.IsNullOrEmpty(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(1)
                .MaximumLength(100)
                .WithMessage("Term name must be between 1 and 100 characters")
                .MustAsync(BeUniqueNameForUpdate).WithMessage("A term with this name already exists");
        });

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        });

        When(x => x.StartDate.HasValue, () =>
        {
            RuleFor(x => x.StartDate)
                .Must(date => BeValidDate(date!.Value))
                .WithMessage("Start date must be a valid date");
        });

        When(x => x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .Must(date => BeValidDate(date!.Value))
                .WithMessage("End date must be a valid date");
        });

        // If both dates are provided, validate that EndDate > StartDate
        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date");
        });

        // Check overlap when dates are being updated
        When(x => x.StartDate.HasValue || x.EndDate.HasValue, () =>
        {
            RuleFor(x => x)
                .MustAsync(NotOverlapWithExistingTerms)
                .WithMessage("The term dates overlap with an existing term")
                .WithName("Term dates");
        });

        // At least one field must be provided for update
        RuleFor(x => x)
            .Must(x => x.Name != null || x.Description != null || x.IsActive.HasValue || x.StartDate.HasValue || x.EndDate.HasValue)
            .WithMessage("At least one field (Name, Description, IsActive, StartDate, or EndDate) must be provided for update");
    }

    private bool BeValidDate(DateTime date)
    {
        return date != default && date.Year >= 1900 && date.Year <= 2100;
    }

    private async Task<bool> BeUniqueNameForUpdate(UpdateTermCommand command, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        var existingTerm = await _termRepository.GetTermByNameAsync(name, cancellationToken);
        return existingTerm == null || existingTerm.Id == command.Id;
    }

    private async Task<bool> NotOverlapWithExistingTerms(UpdateTermCommand command, CancellationToken cancellationToken)
    {
        // Get the existing term to know its current dates
        var existingTerm = await _termRepository.GetAsync(t => t.Id == command.Id, cancellationToken);
        if (existingTerm == null)
            return true; // Let the handler deal with not found

        // Use provided dates or fall back to existing dates
        var startDate = command.StartDate ?? existingTerm.StartDate;
        var endDate = command.EndDate ?? existingTerm.EndDate;

        var overlappingTerm = await _termRepository.GetOverlappingTermAsync(
            startDate,
            endDate,
            command.Id, // Exclude this term from overlap check
            cancellationToken);

        return overlappingTerm == null;
    }
}
