using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Topics.Commands;

/// <summary>
/// Handler for DeleteTopicCommand
/// </summary>
public class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, DeleteTopicResponse>
{
    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<Assignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTopicCommandHandler(
        IRepository<Topic> topicRepository,
        IRepository<Assignment> assignmentRepository,
        IUnitOfWork unitOfWork)
    {
        _topicRepository = topicRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteTopicResponse> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        // Get existing topic
        var topic = await _topicRepository.GetByIdAsync(request.Id, cancellationToken);

        if (topic == null)
        {
            return new DeleteTopicResponse
            {
                Success = false,
                Message = "Topic not found"
            };
        }

        // Check if topic has assignments
        var hasAssignments = await _assignmentRepository.ExistsAsync(
            a => a.TopicId == request.Id,
            cancellationToken);

        if (hasAssignments)
        {
            return new DeleteTopicResponse
            {
                Success = false,
                Message = "Cannot delete topic because it has assignments. Please reassign or delete the assignments first."
            };
        }

        // Delete topic
        await _topicRepository.DeleteAsync(topic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTopicResponse
        {
            Success = true,
            Message = "Topic deleted successfully"
        };
    }
}
