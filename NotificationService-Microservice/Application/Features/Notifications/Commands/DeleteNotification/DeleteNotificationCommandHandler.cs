using MediatR;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Features.Notifications.Commands.DeleteNotification;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteNotificationCommandHandler> _logger;

    public DeleteNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId, cancellationToken);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", request.NotificationId);
                return false;
            }

            if (notification.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete notification {NotificationId} belonging to another user",
                    request.UserId, request.NotificationId);
                return false;
            }

            await _unitOfWork.Notifications.DeleteAsync(request.NotificationId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}",
                request.NotificationId, request.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", request.NotificationId);
            throw;
        }
    }
}
