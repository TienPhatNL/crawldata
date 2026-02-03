using ClassroomService.Domain.Constants;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetSupportRequestByIdQueryHandler : IRequestHandler<GetSupportRequestByIdQuery, GetSupportRequestByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetSupportRequestByIdQueryHandler> _logger;

    public GetSupportRequestByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetSupportRequestByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetSupportRequestByIdResponse> Handle(GetSupportRequestByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var supportRequest = await _unitOfWork.SupportRequests
                .GetSupportRequestByIdAsync(request.SupportRequestId, cancellationToken);

            if (supportRequest == null)
            {
                return new GetSupportRequestByIdResponse
                {
                    Success = false,
                    Message = "Support request not found"
                };
            }

            // Authorization check
            bool isStaff = request.UserRole == RoleConstants.Staff;
            bool isOwner = supportRequest.RequesterId == request.UserId;
            bool isAssignedStaff = supportRequest.AssignedStaffId.HasValue && supportRequest.AssignedStaffId.Value == request.UserId;

            if (!isStaff && !isOwner && !isAssignedStaff)
            {
                return new GetSupportRequestByIdResponse
                {
                    Success = false,
                    Message = "You are not authorized to view this support request"
                };
            }

            var dto = new SupportRequestDto
            {
                Id = supportRequest.Id,
                CourseId = supportRequest.CourseId,
                CourseName = supportRequest.Course?.Name ?? string.Empty,
                RequesterId = supportRequest.RequesterId,
                RequesterName = supportRequest.RequesterName,
                RequesterRole = supportRequest.RequesterRole,
                Priority = supportRequest.Priority,
                Category = supportRequest.Category,
                Subject = supportRequest.Subject,
                Description = supportRequest.Description,
                Status = supportRequest.Status,
                ConversationId = supportRequest.ConversationId,
                AssignedStaffId = supportRequest.AssignedStaffId,
                AssignedStaffName = supportRequest.AssignedStaffName,
                ResolvedAt = supportRequest.ResolvedAt,
                AcceptedAt = supportRequest.AcceptedAt,
                RequestedAt = supportRequest.RequestedAt,
                Images = supportRequest.Images
            };

            return new GetSupportRequestByIdResponse
            {
                Success = true,
                Message = "Support request retrieved successfully",
                SupportRequest = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving support request {SupportRequestId}", request.SupportRequestId);
            return new GetSupportRequestByIdResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the support request"
            };
        }
    }
}

public class GetSupportRequestByIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SupportRequestDto? SupportRequest { get; set; }
}
