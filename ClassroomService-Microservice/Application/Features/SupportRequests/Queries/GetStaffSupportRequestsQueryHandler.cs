using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetStaffSupportRequestsQueryHandler : IRequestHandler<GetStaffSupportRequestsQuery, GetStaffSupportRequestsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetStaffSupportRequestsQueryHandler> _logger;

    public GetStaffSupportRequestsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetStaffSupportRequestsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetStaffSupportRequestsResponse> Handle(GetStaffSupportRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            SupportRequestStatus? status = null;
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<SupportRequestStatus>(request.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

            var result = await _unitOfWork.SupportRequests
                .GetStaffSupportRequestsAsync(request.StaffId, status, request.PageNumber, request.PageSize, cancellationToken);

            var dtoList = result.Data.Select(sr => new SupportRequestDto
            {
                Id = sr.Id,
                CourseId = sr.CourseId,
                CourseName = sr.Course.Name,
                RequesterId = sr.RequesterId,
                RequesterName = sr.RequesterName,
                RequesterRole = sr.RequesterRole,
                AssignedStaffId = sr.AssignedStaffId,
                AssignedStaffName = sr.AssignedStaffName,
                Status = sr.Status,
                Priority = sr.Priority,
                Category = sr.Category,
                Subject = sr.Subject,
                Description = sr.Description,
                ConversationId = sr.ConversationId,
                RequestedAt = sr.RequestedAt,
                AcceptedAt = sr.AcceptedAt,
                ResolvedAt = sr.ResolvedAt
            }).ToList();

            var pagedResult = PagedResult<SupportRequestDto>.Create(
                dtoList, result.TotalCount, result.PageNumber, result.PageSize);

            return new GetStaffSupportRequestsResponse
            {
                Success = true,
                Message = $"Retrieved {dtoList.Count} support requests",
                Data = pagedResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving staff support requests");
            return new GetStaffSupportRequestsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving support requests"
            };
        }
    }
}
