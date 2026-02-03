using FluentValidation;

namespace ClassroomService.Application.Features.Courses.Queries;

public class GetMyCoursesCommandValidator : AbstractValidator<GetMyCoursesCommand>
{
    public GetMyCoursesCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .NotEqual(Guid.Empty).WithMessage("User ID cannot be empty.");

        RuleFor(v => v.Filter.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(v => v.Filter.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(v => v.Filter.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || 
                new[] { "name", "coursecode", "createdat", "enrollmentcount" }
                    .Contains(sortBy.ToLower()))
            .WithMessage("SortBy must be one of: Name, CourseCode, CreatedAt, EnrollmentCount");

        RuleFor(v => v.Filter.SortDirection)
            .Must(direction => string.IsNullOrEmpty(direction) || 
                new[] { "asc", "desc" }.Contains(direction.ToLower()))
            .WithMessage("SortDirection must be either 'asc' or 'desc'");

        RuleFor(v => v.Filter.MinEnrollmentCount)
            .GreaterThanOrEqualTo(0).When(v => v.Filter.MinEnrollmentCount.HasValue)
            .WithMessage("Minimum enrollment count must be 0 or greater.");

        RuleFor(v => v.Filter.MaxEnrollmentCount)
            .GreaterThanOrEqualTo(0).When(v => v.Filter.MaxEnrollmentCount.HasValue)
            .WithMessage("Maximum enrollment count must be 0 or greater.");

        RuleFor(v => v.Filter)
            .Must(filter => !filter.MinEnrollmentCount.HasValue || !filter.MaxEnrollmentCount.HasValue ||
                filter.MinEnrollmentCount.Value <= filter.MaxEnrollmentCount.Value)
            .WithMessage("Minimum enrollment count must be less than or equal to maximum enrollment count.");

        RuleFor(v => v.Filter)
            .Must(filter => !filter.CreatedAfter.HasValue || !filter.CreatedBefore.HasValue ||
                filter.CreatedAfter.Value <= filter.CreatedBefore.Value)
            .WithMessage("CreatedAfter must be less than or equal to CreatedBefore.");
    }
}