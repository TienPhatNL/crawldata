using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.Notifications.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, IEnumerable<Notification>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Notification>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Notifications.GetUserNotificationsAsync(
            request.UserId,
            request.IsRead,
            request.Take,
            request.IsStaff,
            cancellationToken);
    }
}
