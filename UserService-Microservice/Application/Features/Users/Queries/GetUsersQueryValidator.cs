using FluentValidation;

namespace UserService.Application.Features.Users.Queries;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || IsValidSortField(sortBy))
            .WithMessage("Invalid sort field. Valid fields are: Email, FirstName, LastName, Role, Status, CreatedAt, LastLoginAt");

        RuleFor(x => x.SortOrder)
            .Must(order => string.IsNullOrEmpty(order) || order.ToLower() == "asc" || order.ToLower() == "desc")
            .WithMessage("Sort order must be 'asc' or 'desc'");
    }

    private static bool IsValidSortField(string sortBy)
    {
        var validFields = new[] { "Email", "FirstName", "LastName", "Role", "Status", "CreatedAt", "LastLoginAt" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
}