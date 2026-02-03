using MediatR;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Queries;

/// <summary>
/// Handler for GetTopicByIdQuery
/// </summary>
public class GetTopicByIdQueryHandler : IRequestHandler<GetTopicByIdQuery, GetTopicByIdResponse>
{
    private readonly IRepository<Topic> _topicRepository;

    public GetTopicByIdQueryHandler(IRepository<Topic> topicRepository)
    {
        _topicRepository = topicRepository;
    }

    public async Task<GetTopicByIdResponse> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topicRepository.GetByIdAsync(request.Id, cancellationToken);

        if (topic == null)
        {
            return new GetTopicByIdResponse
            {
                Success = false,
                Message = "Topic not found"
            };
        }

        return new GetTopicByIdResponse
        {
            Success = true,
            Message = "Topic retrieved successfully",
            Topic = new TopicDto
            {
                Id = topic.Id,
                Name = topic.Name,
                Description = topic.Description,
                IsActive = topic.IsActive,
                CreatedAt = topic.CreatedAt,
                UpdatedAt = topic.UpdatedAt,
                CreatedBy = topic.CreatedBy,
                LastModifiedBy = topic.LastModifiedBy,
                LastModifiedAt = topic.LastModifiedAt
            }
        };
    }
}
