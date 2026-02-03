using MediatR;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

    public MarkNotificationAsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkNotificationAsReadCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
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
                _logger.LogWarning("User {UserId} attempted to mark notification {NotificationId} belonging to another user", 
                    request.UserId, request.NotificationId);
                return false;
            }

            if (notification.IsRead)
            {
                _logger.LogInformation("Notification {NotificationId} is already marked as read", request.NotificationId);
                return true;
            }

            await _unitOfWork.Notifications.MarkAsReadAsync(request.NotificationId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked notification {NotificationId} as read for user {UserId}", 
                request.NotificationId, request.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", request.NotificationId);
            throw;
        }
    }
}
