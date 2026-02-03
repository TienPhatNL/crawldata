using MediatR;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Handler for GetTopicsDropdownQuery
/// </summary>
public class GetTopicsDropdownQueryHandler : IRequestHandler<GetTopicsDropdownQuery, GetTopicsDropdownResponse>
{
    private readonly IRepository<Topic> _topicRepository;

    public GetTopicsDropdownQueryHandler(IRepository<Topic> topicRepository)
    {
        _topicRepository = topicRepository;
    }

    public async Task<GetTopicsDropdownResponse> Handle(GetTopicsDropdownQuery request, CancellationToken cancellationToken)
    {
        // Get only active topics
        var topics = await _topicRepository.GetManyAsync(
            t => t.IsActive,
            cancellationToken);

        // Order by name
        var orderedTopics = topics.OrderBy(t => t.Name).ToList();

        var topicDtos = orderedTopics.Select(t => new TopicDropdownDto
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        return new GetTopicsDropdownResponse
        {
            Success = true,
            Message = "Active topics retrieved successfully",
            Topics = topicDtos
        };
    }
}
