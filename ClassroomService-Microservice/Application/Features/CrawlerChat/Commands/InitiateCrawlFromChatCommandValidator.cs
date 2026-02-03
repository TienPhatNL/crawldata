using FluentValidation;

namespace ClassroomService.Application.Features.CrawlerChat.Commands;

public class InitiateCrawlFromChatCommandValidator : AbstractValidator<InitiateCrawlFromChatCommand>
{
    public InitiateCrawlFromChatCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required");

        RuleFor(x => x.SenderId)
            .NotEmpty()
            .WithMessage("Sender ID is required");

        RuleFor(x => x.MessageContent)
            .NotEmpty()
            .WithMessage("Message content cannot be empty")
            .MaximumLength(2000)
            .WithMessage("Message content cannot exceed 2000 characters");

        RuleFor(x => x.SenderName)
            .NotEmpty()
            .WithMessage("Sender name is required")
            .MaximumLength(100)
            .WithMessage("Sender name cannot exceed 100 characters");
    }
}
