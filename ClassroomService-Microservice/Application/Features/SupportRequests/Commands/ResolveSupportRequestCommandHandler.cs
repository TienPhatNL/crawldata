using MediatR;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class ResolveSupportRequestCommandHandler : IRequestHandler<ResolveSupportRequestCommand, ResolveSupportRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResolveSupportRequestCommandHandler> _logger;

    public ResolveSupportRequestCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ResolveSupportRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ResolveSupportRequestResponse> Handle(ResolveSupportRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var supportRequest = await _unitOfWork.SupportRequests
                .GetSupportRequestByIdAsync(request.SupportRequestId, cancellationToken);

            if (supportRequest == null)
            {
                return new ResolveSupportRequestResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Only requester can resolve
            if (supportRequest.RequesterId != request.UserId)
            {
                return new ResolveSupportRequestResponse
                {
                    Success = false,
                    Message = "Only the requester can resolve this support request"
                };
            }

            // Must be InProgress to resolve
            if (supportRequest.Status != SupportRequestStatus.InProgress)
            {
                return new ResolveSupportRequestResponse
                {
                    Success = false,
                    Message = $"Cannot resolve a support request with status: {supportRequest.Status}"
                };
            }

            supportRequest.Status = SupportRequestStatus.Resolved;
            supportRequest.ResolvedAt = DateTime.UtcNow;
            supportRequest.LastModifiedAt = DateTime.UtcNow;
            supportRequest.LastModifiedBy = request.UserId;

            // Close the conversation if it exists
            if (supportRequest.ConversationId.HasValue)
            {
                var conversation = await _unitOfWork.Conversations
                    .GetByIdAsync(supportRequest.ConversationId.Value, cancellationToken);
                
                if (conversation != null)
                {
                    conversation.IsClosed = true;
                    conversation.ClosedAt = DateTime.UtcNow;
                    conversation.ClosedBy = request.UserId;
                }
            }

            // Raise domain event (will be auto-dispatched by UnitOfWork.SaveChangesAsync)
            supportRequest.AddDomainEvent(new SupportRequestResolvedEvent(
                supportRequest.Id,
                supportRequest.CourseId,
                supportRequest.Course.Name,
                supportRequest.RequesterId,
                supportRequest.RequesterName,
                supportRequest.AssignedStaffId,
                supportRequest.AssignedStaffName,
                request.UserId,
                supportRequest.Subject
            ));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Support request {RequestId} resolved by user {UserId}", 
                supportRequest.Id, request.UserId);

            return new ResolveSupportRequestResponse
            {
                Success = true,
                Message = "Support request resolved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving support request");
            return new ResolveSupportRequestResponse
            {
                Success = false,
                Message = "An error occurred while resolving the support request"
            };
        }
    }
}
