using FluentValidation;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementByIdQueryValidator : AbstractValidator<GetAnnouncementByIdQuery>
{
    public GetAnnouncementByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}
