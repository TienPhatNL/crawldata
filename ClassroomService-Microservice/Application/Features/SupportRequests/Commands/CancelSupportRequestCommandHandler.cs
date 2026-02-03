using MediatR;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class CancelSupportRequestCommandHandler : IRequestHandler<CancelSupportRequestCommand, CancelSupportRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSupportRequestCommandHandler> _logger;

    public CancelSupportRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CancelSupportRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CancelSupportRequestResponse> Handle(CancelSupportRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var supportRequest = await _unitOfWork.SupportRequests
                .GetSupportRequestByIdAsync(request.SupportRequestId, cancellationToken);

            if (supportRequest == null)
            {
                return new CancelSupportRequestResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Only requester can cancel
            if (supportRequest.RequesterId != request.UserId)
            {
                return new CancelSupportRequestResponse
                {
                    Success = false,
                    Message = "You can only cancel your own support requests"
                };
            }

            // Can only cancel if Pending
            if (supportRequest.Status != SupportRequestStatus.Pending)
            {
                return new CancelSupportRequestResponse
                {
                    Success = false,
                    Message = $"Cannot cancel a support request with status: {supportRequest.Status}"
                };
            }

            supportRequest.Status = SupportRequestStatus.Cancelled;
            supportRequest.LastModifiedAt = DateTime.UtcNow;
            supportRequest.LastModifiedBy = request.UserId;

            // Raise domain event (only if there was an assigned staff to notify)
            // Will be auto-dispatched by UnitOfWork.SaveChangesAsync
            if (supportRequest.AssignedStaffId.HasValue)
            {
                supportRequest.AddDomainEvent(new SupportRequestCancelledEvent(
                    supportRequest.Id,
                    supportRequest.CourseId,
                    supportRequest.Course.Name,
                    supportRequest.RequesterId,
                    supportRequest.RequesterName,
                    supportRequest.Subject
                ));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Support request {RequestId} cancelled by user {UserId}", 
                supportRequest.Id, request.UserId);

            return new CancelSupportRequestResponse
            {
                Success = true,
                Message = "Support request cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling support request");
            return new CancelSupportRequestResponse
            {
                Success = false,
                Message = "An error occurred while cancelling the support request"
            };
        }
    }
}
