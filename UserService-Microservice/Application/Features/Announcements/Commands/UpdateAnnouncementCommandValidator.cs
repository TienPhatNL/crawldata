using FluentValidation;
using UserService.Application.Features.Announcements;

namespace UserService.Application.Features.Announcements.Commands;

public class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(AnnouncementValidationConstants.MaxContentLength)
            .WithMessage($"Content cannot exceed {AnnouncementValidationConstants.MaxContentLength} characters");
    }
}
