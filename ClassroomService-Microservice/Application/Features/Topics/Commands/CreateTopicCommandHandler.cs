using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Handler for CreateTopicCommand
/// </summary>
public class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, CreateTopicResponse>
{
    private readonly IRepository<Topic> _topicRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTopicCommandHandler(
        IRepository<Topic> topicRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _topicRepository = topicRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateTopicResponse> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        // Check if topic name already exists
        var existingTopic = await _topicRepository.GetAsync(
            t => t.Name.ToLower() == request.Name.ToLower(),
            cancellationToken);

        if (existingTopic != null)
        {
            return new CreateTopicResponse
            {
                Success = false,
                Message = $"Topic with name '{request.Name}' already exists"
            };
        }

        // Create new topic
        var topic = new Topic
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedBy = _currentUserService.UserId
        };

        await _topicRepository.AddAsync(topic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateTopicResponse
        {
            Success = true,
            Message = "Topic created successfully",
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
