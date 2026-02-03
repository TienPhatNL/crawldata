using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Chat.Commands;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, DeleteMessageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMessageCommandHandler> _logger;

    public DeleteMessageCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteMessageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeleteMessageResponse> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _unitOfWork.Chats.GetByIdAsync(request.MessageId);
            if (message == null)
            {
                return new DeleteMessageResponse
                {
                    Success = false,
                    Message = "Message not found"
                };
            }

            // Only sender can delete
            if (message.SenderId != request.UserId)
            {
                return new DeleteMessageResponse
                {
                    Success = false,
                    Message = "You can only delete messages you sent"
                };
            }

            message.IsDeleted = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} deleted message {MessageId}", request.UserId, request.MessageId);

            return new DeleteMessageResponse
            {
                Success = true,
                Message = "Message deleted successfully",
                ReceiverId = message.ReceiverId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", request.MessageId);
            return new DeleteMessageResponse
            {
                Success = false,
                Message = $"Error deleting message: {ex.Message}"
            };
        }
    }
}
