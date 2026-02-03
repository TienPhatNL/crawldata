using MediatR;
using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Query to get active topics for dropdown selection
/// </summary>
public class GetTopicsDropdownQuery : IRequest<GetTopicsDropdownResponse>
{
}

/// <summary>
/// Response for get topics dropdown query
/// </summary>
public class GetTopicsDropdownResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TopicDropdownDto> Topics { get; set; } = new();
}
