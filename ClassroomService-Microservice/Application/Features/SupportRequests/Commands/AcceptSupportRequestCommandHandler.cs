using MediatR;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Commands;

public class AcceptSupportRequestCommandHandler : IRequestHandler<AcceptSupportRequestCommand, AcceptSupportRequestResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<AcceptSupportRequestCommandHandler> _logger;

    public AcceptSupportRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IKafkaUserService userService,
        ILogger<AcceptSupportRequestCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _logger = logger;
    }

    public async Task<AcceptSupportRequestResponse> Handle(AcceptSupportRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate staff role
            var isStaff = await _userService.ValidateUserAsync(request.StaffId, "Staff", cancellationToken);
            if (!isStaff)
            {
                return new AcceptSupportRequestResponse
                {
                    Success = false,
                    Message = "Only staff members can accept support requests"
                };
            }

            // Get support request
            var supportRequest = await _unitOfWork.SupportRequests
                .GetSupportRequestByIdAsync(request.SupportRequestId, cancellationToken);

            if (supportRequest == null)
            {
                return new AcceptSupportRequestResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Check if already accepted
            if (supportRequest.Status != SupportRequestStatus.Pending)
            {
                return new AcceptSupportRequestResponse
                {
                    Success = false,
                    Message = $"Support request is already {supportRequest.Status}"
                };
            }

            // Get staff name
            var staffInfo = await _userService.GetUserByIdAsync(request.StaffId, cancellationToken);
            var staffName = staffInfo?.FullName ?? "Staff Member";

            // Check for existing conversation between these users in this course
            var existingConversation = await _unitOfWork.Conversations
                .GetConversationAsync(supportRequest.CourseId, supportRequest.RequesterId, request.StaffId, cancellationToken);

            Conversation conversation;
            
            if (existingConversation != null)
            {
                // Reopen and reset the existing conversation for this new support request
                // Old messages remain in database but are filtered by SupportRequestId in queries
                conversation = existingConversation;
                conversation.IsClosed = false;
                conversation.ClosedAt = null;
                conversation.ClosedBy = null;
                conversation.LastMessageAt = DateTime.UtcNow;
                
                // Truncate preview to 100 chars (database column limit)
                var previewText = $"New support request: {supportRequest.Subject}";
                conversation.LastMessagePreview = previewText.Length > 100 
                    ? previewText.Substring(0, 97) + "..." 
                    : previewText;
                
                conversation.LastModifiedAt = DateTime.UtcNow;
                conversation.LastModifiedBy = request.StaffId;
                
                // Update the conversation in the database
                await _unitOfWork.Conversations.UpdateAsync(conversation, cancellationToken);
            }
            else
            {
                // Create new conversation if none exists
                var user1Id = supportRequest.RequesterId < request.StaffId ? supportRequest.RequesterId : request.StaffId;
                var user2Id = supportRequest.RequesterId < request.StaffId ? request.StaffId : supportRequest.RequesterId;

                // Truncate preview to 100 chars (database column limit)
                var previewText = $"Support request accepted: {supportRequest.Subject}";
                var truncatedPreview = previewText.Length > 100 
                    ? previewText.Substring(0, 97) + "..." 
                    : previewText;

                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    CourseId = supportRequest.CourseId,
                    User1Id = user1Id,
                    User2Id = user2Id,
                    LastMessageAt = DateTime.UtcNow,
                    LastMessagePreview = truncatedPreview,
                    IsClosed = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.StaffId
                };

                await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
            }

            // Update support request - change from Pending to InProgress
            supportRequest.AssignedStaffId = request.StaffId;
            supportRequest.AssignedStaffName = staffName;
            supportRequest.Status = SupportRequestStatus.InProgress;
            supportRequest.AcceptedAt = DateTime.UtcNow;
            supportRequest.ConversationId = conversation.Id;
            supportRequest.LastModifiedAt = DateTime.UtcNow;
            supportRequest.LastModifiedBy = request.StaffId;

            // Raise domain event (will be auto-dispatched by UnitOfWork.SaveChangesAsync)
            supportRequest.AddDomainEvent(new SupportRequestAcceptedEvent(
                supportRequest.Id,
                supportRequest.CourseId,
                supportRequest.Course.Name,
                supportRequest.RequesterId,
                supportRequest.RequesterName,
                request.StaffId,
                staffName,
                conversation.Id,
                supportRequest.Subject
            ));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Support request {RequestId} accepted by staff {StaffId}", 
                supportRequest.Id, request.StaffId);

            return new AcceptSupportRequestResponse
            {
                Success = true,
                Message = "Support request accepted successfully. Conversation created.",
                ConversationId = conversation.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting support request");
            return new AcceptSupportRequestResponse
            {
                Success = false,
                Message = "An error occurred while accepting the support request"
            };
        }
    }
}
