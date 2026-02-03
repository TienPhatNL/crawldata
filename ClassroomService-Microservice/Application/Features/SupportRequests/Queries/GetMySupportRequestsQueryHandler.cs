using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetMySupportRequestsQueryHandler : IRequestHandler<GetMySupportRequestsQuery, GetMySupportRequestsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMySupportRequestsQueryHandler> _logger;

    public GetMySupportRequestsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetMySupportRequestsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetMySupportRequestsResponse> Handle(GetMySupportRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            SupportRequestStatus? status = null;
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<SupportRequestStatus>(request.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

            var result = await _unitOfWork.SupportRequests
                .GetMySupportRequestsAsync(request.UserId, request.CourseId, status, request.PageNumber, request.PageSize, cancellationToken);

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
                Images = sr.Images,
                ConversationId = sr.ConversationId,
                RequestedAt = sr.RequestedAt,
                AcceptedAt = sr.AcceptedAt,
                ResolvedAt = sr.ResolvedAt
            }).ToList();

            var pagedResult = PagedResult<SupportRequestDto>.Create(
                dtoList, result.TotalCount, result.PageNumber, result.PageSize);

            return new GetMySupportRequestsResponse
            {
                Success = true,
                Message = $"Retrieved {dtoList.Count} support requests",
                Data = pagedResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user support requests");
            return new GetMySupportRequestsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving your support requests"
            };
        }
    }
}
