using ClassroomService.Application.Features.Terms.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Terms.Queries;

/// <summary>
/// Handles the GetAllTermsQuery to retrieve a list of terms with optional filters and pagination
/// </summary>
public class GetAllTermsQueryHandler : IRequestHandler<GetAllTermsQuery, GetAllTermsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllTermsQueryHandler> _logger;

    public GetAllTermsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllTermsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetAllTermsResponse> Handle(GetAllTermsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination parameters
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

            // Get all terms or only active ones
            var terms = request.ActiveOnly.HasValue && request.ActiveOnly.Value
                ? await _unitOfWork.Terms.GetActiveTermsAsync(cancellationToken)
                : await _unitOfWork.Terms.GetAllAsync(cancellationToken);
            
            var query = terms.AsQueryable();

            // Filter by active status if specified (and not already filtered by GetActiveTermsAsync)
            if (request.ActiveOnly.HasValue && !request.ActiveOnly.Value)
            {
                query = query.Where(t => !t.IsActive);
            }

            // Filter by name (case-insensitive, partial match)
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(name));
            }

            // Get total count before pagination
            var totalCount = query.Count();

            // Apply sorting
            var sortBy = request.SortBy?.ToLower() ?? "name";
            var sortDirection = request.SortDirection?.ToLower() ?? "asc";

            query = sortBy switch
            {
                "createdat" => sortDirection == "desc"
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt),
                "updatedat" => sortDirection == "desc"
                    ? query.OrderByDescending(t => t.UpdatedAt)
                    : query.OrderBy(t => t.UpdatedAt),
                "name" or _ => sortDirection == "desc"
                    ? query.OrderByDescending(t => t.Name)
                    : query.OrderBy(t => t.Name)
            };

            // Apply pagination
            var paginatedTerms = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TermDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToList();

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var hasPreviousPage = page > 1;
            var hasNextPage = page < totalPages;

            _logger.LogInformation("Retrieved {Count} terms (page {Page} of {TotalPages})", paginatedTerms.Count, page, totalPages);

            return new GetAllTermsResponse
            {
                Success = true,
                Message = $"Terms retrieved successfully (page {page} of {totalPages})",
                Terms = paginatedTerms,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = hasPreviousPage,
                HasNextPage = hasNextPage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terms");
            return new GetAllTermsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving terms",
                Terms = new List<TermDto>(),
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                HasPreviousPage = false,
                HasNextPage = false
            };
        }
    }
}

