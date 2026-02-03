using MediatR;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class RejectSupportRequestCommandHandler : IRequestHandler<RejectSupportRequestCommand, RejectSupportRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<RejectSupportRequestCommandHandler> _logger;

    public RejectSupportRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<RejectSupportRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<RejectSupportRequestResponse> Handle(RejectSupportRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate staff role
            var isStaff = await _userService.ValidateUserAsync(request.StaffId, "Staff", cancellationToken);
            if (!isStaff)
            {
                return new RejectSupportRequestResponse
                {
                    Success = false,
                    Message = "Only staff members can reject support requests"
                };
            }

            // Get support request
            var supportRequest = await _unitOfWork.SupportRequests
                .GetSupportRequestByIdAsync(request.SupportRequestId, cancellationToken);

            if (supportRequest == null)
            {
                return new RejectSupportRequestResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Can only reject pending requests
            if (supportRequest.Status != SupportRequestStatus.Pending)
            {
                return new RejectSupportRequestResponse
                {
                    Success = false,
                    Message = $"Cannot reject a support request with status: {supportRequest.Status}"
                };
            }

            // Get staff name
            var staffInfo = await _userService.GetUserByIdAsync(request.StaffId, cancellationToken);
            var staffName = staffInfo?.FullName ?? "Staff Member";

            // Update support request
            supportRequest.Status = SupportRequestStatus.Rejected;
            supportRequest.RejectionReason = request.RejectionReason;
            supportRequest.RejectionComments = request.RejectionComments;
            supportRequest.RejectedBy = request.StaffId;
            supportRequest.RejectedAt = DateTime.UtcNow;
            supportRequest.LastModifiedAt = DateTime.UtcNow;
            supportRequest.LastModifiedBy = request.StaffId;

            // Raise domain event (will be auto-dispatched by UnitOfWork.SaveChangesAsync)
            supportRequest.AddDomainEvent(new SupportRequestRejectedEvent(
                supportRequest.Id,
                supportRequest.CourseId,
                supportRequest.Course.Name,
                supportRequest.RequesterId,
                supportRequest.RequesterName,
                request.StaffId,
                staffName,
                request.RejectionReason,
                request.RejectionComments,
                supportRequest.Subject
            ));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Support request {RequestId} rejected by staff {StaffId} with reason {Reason}",
                supportRequest.Id, request.StaffId, request.RejectionReason);

            return new RejectSupportRequestResponse
            {
                Success = true,
                Message = "Support request rejected successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting support request");
            return new RejectSupportRequestResponse
            {
                Success = false,
                Message = "An error occurred while rejecting the support request"
            };
        }
    }
}
