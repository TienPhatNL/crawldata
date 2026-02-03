using MediatR;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Query to get all topics with optional filters and pagination
/// </summary>
public class GetAllTopicsQuery : IRequest<GetAllTopicsResponse>
{
    /// <summary>
    /// Filter by topic name (partial match, case-insensitive)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter to only active topics (true = active, false = inactive, null = all)
    /// </summary>
    public bool? IsActive { get; set; }

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
