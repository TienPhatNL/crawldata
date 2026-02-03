using FluentValidation;
using UserService.Domain.Enums;

namespace UserService.Application.Features.Announcements.Queries;

public class GetAnnouncementsQueryValidator : AbstractValidator<GetAnnouncementsQuery>
{
    public GetAnnouncementsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

      
        RuleForEach(x => x.Audiences)
            .IsInEnum().WithMessage("Audience is not valid")
            .Must(a =>
                a == AnnouncementAudience.All ||
                a == AnnouncementAudience.Students ||
                a == AnnouncementAudience.Lecturers)
            .WithMessage("Audience is not supported");
    }
}
