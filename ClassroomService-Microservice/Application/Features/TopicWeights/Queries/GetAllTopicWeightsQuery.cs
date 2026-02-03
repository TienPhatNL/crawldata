using MediatR;
using ClassroomService.Application.Features.TopicWeights.DTOs;
using ClassroomService.Domain.Common;

namespace ClassroomService.Application.Features.TopicWeights.Queries;

public class GetAllTopicWeightsQuery : IRequest<PagedResult<TopicWeightResponseDto>>
{
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Filters
    public string? CourseCode { get; set; }
    public string? TopicName { get; set; }
    public string? CourseName { get; set; }
    public Guid? CourseCodeId { get; set; }
    public Guid? SpecificCourseId { get; set; }
    public Guid? TopicId { get; set; }
    
    /// <summary>
    /// Filter to show only weights that can be edited (not in active terms)
    /// </summary>
    public bool? CanEdit { get; set; }
}
