using MediatR;
using ClassroomService.Domain.Common;
using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.SupportRequests.Queries;

public class GetPendingSupportRequestsQueryHandler : IRequestHandler<GetPendingSupportRequestsQuery, GetPendingSupportRequestsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPendingSupportRequestsQueryHandler> _logger;

    public GetPendingSupportRequestsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPendingSupportRequestsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetPendingSupportRequestsResponse> Handle(GetPendingSupportRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _unitOfWork.SupportRequests
                .GetPendingSupportRequestsAsync(request.CourseId, request.PageNumber, request.PageSize, cancellationToken);

            var dtoList = result.Data.Select(sr => new SupportRequestListDto
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
                RequestedAt = sr.RequestedAt
            }).ToList();

            var pagedResult = PagedResult<SupportRequestListDto>.Create(
                dtoList, result.TotalCount, result.PageNumber, result.PageSize);

            return new GetPendingSupportRequestsResponse
            {
                Success = true,
                Message = $"Retrieved {dtoList.Count} pending support requests",
                Data = pagedResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending support requests");
            return new GetPendingSupportRequestsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving support requests"
            };
        }
    }
}
