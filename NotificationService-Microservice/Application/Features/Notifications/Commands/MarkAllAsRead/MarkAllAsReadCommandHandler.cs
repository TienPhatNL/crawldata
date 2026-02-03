using MediatR;
using NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkAllAsReadCommandHandler> _logger;

    public MarkAllAsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<MarkAllAsReadCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.Notifications.MarkAllAsReadAsync(request.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked all notifications as read for user {UserId}", request.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", request.UserId);
            throw;
        }
    }
}
