using FluentValidation;

namespace ClassroomService.Application.Features.Assignments.Commands;

public class AssignGroupsCommandValidator : AbstractValidator<AssignGroupsCommand>
{
    public AssignGroupsCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty()
            .WithMessage("Assignment ID is required");

        RuleFor(x => x.GroupIds)
            .NotNull()
            .WithMessage("Group IDs list cannot be null")
            .Must(ids => ids.Count > 0)
            .WithMessage("At least one group must be specified")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate group IDs are not allowed");
    }
}
