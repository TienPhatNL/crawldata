using MediatR;
using ClassroomService.Application.Features.Terms.DTOs;

namespace ClassroomService.Application.Features.Terms.Queries;

/// <summary>
/// Query to get all terms with optional filters and pagination
/// </summary>
public class GetAllTermsQuery : IRequest<GetAllTermsResponse>
{
    /// <summary>
    /// Filter to only active terms (true = active, false = inactive, null = all)
    /// </summary>
    public bool? ActiveOnly { get; set; }

    /// <summary>
    /// Filter by term name (partial match, case-insensitive)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Page number (starting from 1)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Sort field (Name, CreatedAt)
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Response for get all terms query
/// </summary>
public class GetAllTermsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TermDto> Terms { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
