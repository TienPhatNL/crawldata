using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Features.Notifications.Queries.GetNotificationById;

public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, Notification?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationByIdQueryHandler> _logger;

    public GetNotificationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Notification?> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", request.NotificationId);
            return null;
        }

        if (notification.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to access notification {NotificationId} belonging to another user",
                request.UserId, request.NotificationId);
            return null;
        }

        return notification;
    }
}
