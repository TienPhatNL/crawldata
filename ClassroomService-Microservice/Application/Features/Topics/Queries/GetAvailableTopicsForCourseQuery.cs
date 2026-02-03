using MediatR;
using ClassroomService.Application.Features.Topics.DTOs;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Query to get topics with configured weights for a specific course
/// </summary>
public class GetAvailableTopicsForCourseQuery : IRequest<GetAvailableTopicsForCourseResponse>
{
    public Guid CourseId { get; set; }
}

/// <summary>
/// Response for available topics query
/// </summary>
public class GetAvailableTopicsForCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TopicWithWeightDto> Topics { get; set; } = new();
    public bool HasCustomWeights { get; set; } // True if course has specific weights, false if using course code defaults
}
