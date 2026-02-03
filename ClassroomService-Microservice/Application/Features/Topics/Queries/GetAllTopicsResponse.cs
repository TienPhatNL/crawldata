using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Response for get all topics query
/// </summary>
public class GetAllTopicsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TopicDto> Topics { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
