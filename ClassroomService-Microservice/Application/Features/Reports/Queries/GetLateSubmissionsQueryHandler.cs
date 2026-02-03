using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetLateSubmissionsQueryHandler : IRequestHandler<GetLateSubmissionsQuery, GetLateSubmissionsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetLateSubmissionsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetLateSubmissionsResponse> Handle(GetLateSubmissionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = _currentUserService.Role;

            // Only Lecturer can access
            if (userRole != RoleConstants.Lecturer)
            {
                return new GetLateSubmissionsResponse
                {
                    Success = false,
                    Message = "Access denied. Only lecturers can view late submissions."
                };
            }

            // Get late submissions
            var lateReports = await _unitOfWork.Reports.GetLateSubmissionsByCourseAsync(request.CourseId, request.AssignmentId, cancellationToken);

            var totalCount = lateReports.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination and map to DTO
            var paginatedReports = lateReports
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r =>
                {
                    var daysLate = 0;
                    if (r.SubmittedAt.HasValue)
                    {
                        var timeSpan = r.SubmittedAt.Value - r.Assignment.DueDate;
                        daysLate = (int)Math.Ceiling(timeSpan.TotalDays);
                    }

                    return new LateReportDto
                    {
                        Id = r.Id,
                        AssignmentId = r.AssignmentId,
                        AssignmentTitle = r.Assignment.Title,
                        GroupId = r.GroupId,
                        GroupName = r.Group?.Name,
                        SubmittedBy = r.SubmittedBy,
                        SubmittedAt = r.SubmittedAt,
                        Status = r.Status,
                        Grade = r.Grade,
                        GradedBy = r.GradedBy,
                        GradedAt = r.GradedAt,
                        IsGroupSubmission = r.IsGroupSubmission,
                        Version = r.Version,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        Deadline = r.Assignment.DueDate,
                        DaysLate = daysLate
                    };
                })
                .ToList();

            return new GetLateSubmissionsResponse
            {
                Success = true,
                Message = "Late submissions retrieved successfully",
                Reports = paginatedReports,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };
        }
        catch (Exception ex)
        {
            return new GetLateSubmissionsResponse
            {
                Success = false,
                Message = $"Error retrieving late submissions: {ex.Message}"
            };
        }
    }
}
