using FluentValidation;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid notification type");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Invalid event source");

        RuleFor(x => x.Channels)
            .NotEmpty().WithMessage("At least one notification channel is required")
            .Must(channels => channels.All(c => c == NotificationChannel.InApp || c == NotificationChannel.Email))
            .WithMessage("Only InApp and Email channels are supported");

        RuleFor(x => x.ExpiresAt)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future");
    }
}
