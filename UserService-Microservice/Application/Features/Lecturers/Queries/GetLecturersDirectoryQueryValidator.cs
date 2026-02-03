using FluentValidation;

namespace UserService.Application.Features.Lecturers.Queries;

public class GetLecturersDirectoryQueryValidator : AbstractValidator<GetLecturersDirectoryQuery>
{
    public GetLecturersDirectoryQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}
