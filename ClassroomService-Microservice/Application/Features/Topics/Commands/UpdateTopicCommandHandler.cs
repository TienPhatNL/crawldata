using MediatR;
using Microsoft.EntityFrameworkCore;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Topics.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Handler for UpdateTopicCommand
/// </summary>
public class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, UpdateTopicResponse>
{
    private readonly IRepository<Topic> _topicRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTopicCommandHandler(
        IRepository<Topic> topicRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _topicRepository = topicRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateTopicResponse> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
    {
        // Get existing topic
        var topic = await _topicRepository.GetByIdAsync(request.Id, cancellationToken);

        if (topic == null)
        {
            return new UpdateTopicResponse
            {
                Success = false,
                Message = "Topic not found"
            };
        }

        // Check if new name conflicts with another topic
        var existingTopic = await _topicRepository.GetAsync(
            t => t.Name.ToLower() == request.Name.ToLower() && t.Id != request.Id,
            cancellationToken);

        if (existingTopic != null)
        {
            return new UpdateTopicResponse
            {
                Success = false,
                Message = $"Another topic with name '{request.Name}' already exists"
            };
        }

        // Update topic
        topic.Name = request.Name;
        topic.Description = request.Description;
        topic.IsActive = request.IsActive;
        topic.LastModifiedBy = _currentUserService.UserId;

        await _topicRepository.UpdateAsync(topic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateTopicResponse
        {
            Success = true,
            Message = "Topic updated successfully",
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
