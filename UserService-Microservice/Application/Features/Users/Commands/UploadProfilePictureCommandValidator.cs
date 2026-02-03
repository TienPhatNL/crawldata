using FluentValidation;

namespace UserService.Application.Features.Users.Commands;

public class UploadProfilePictureCommandValidator : AbstractValidator<UploadProfilePictureCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public UploadProfilePictureCommandValidator()
    {
        RuleFor(x => x.ProfilePicture)
            .NotNull().WithMessage("Profile picture is required")
            .Must(file => file == null || file.Length > 0)
            .WithMessage("Profile picture cannot be empty")
            .Must(file => file == null || file.Length <= MaxFileSizeBytes)
            .WithMessage("Profile picture cannot exceed 5MB");
    }
}
