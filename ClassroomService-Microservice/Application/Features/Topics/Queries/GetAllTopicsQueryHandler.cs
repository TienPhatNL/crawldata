using MediatR;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Handler for GetAllTopicsQuery
/// </summary>
public class GetAllTopicsQueryHandler : IRequestHandler<GetAllTopicsQuery, GetAllTopicsResponse>
{
    private readonly IRepository<Topic> _topicRepository;

    public GetAllTopicsQueryHandler(IRepository<Topic> topicRepository)
    {
        _topicRepository = topicRepository;
    }

    public async Task<GetAllTopicsResponse> Handle(GetAllTopicsQuery request, CancellationToken cancellationToken)
    {
        // Build filter expression
        System.Linq.Expressions.Expression<Func<Topic, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(request.Name) || request.IsActive.HasValue)
        {
            filter = t => 
                (string.IsNullOrWhiteSpace(request.Name) || t.Name.ToLower().Contains(request.Name.ToLower())) &&
                (!request.IsActive.HasValue || t.IsActive == request.IsActive.Value);
        }

        // Build sorting
        Func<IQueryable<Topic>, IOrderedQueryable<Topic>> orderBy = request.SortBy.ToLower() switch
        {
            "createdat" => request.SortDirection.ToLower() == "desc" 
                ? q => q.OrderByDescending(t => t.CreatedAt)
                : q => q.OrderBy(t => t.CreatedAt),
            _ => request.SortDirection.ToLower() == "desc"
                ? q => q.OrderByDescending(t => t.Name)
                : q => q.OrderBy(t => t.Name)
        };

        // Get paginated results
        var pagedResult = await _topicRepository.GetPagedAsync(
            filter: filter,
            orderBy: orderBy,
            pageNumber: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        // Map to DTOs
        var topicDtos = pagedResult.Data.Select(t => new TopicDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            CreatedBy = t.CreatedBy,
            LastModifiedBy = t.LastModifiedBy,
            LastModifiedAt = t.LastModifiedAt
        }).ToList();

        return new GetAllTopicsResponse
        {
            Success = true,
            Message = "Topics retrieved successfully",
            Topics = topicDtos,
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage
        };
    }
}
