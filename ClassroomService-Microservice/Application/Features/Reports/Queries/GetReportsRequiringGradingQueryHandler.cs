using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsRequiringGradingQueryHandler : IRequestHandler<GetReportsRequiringGradingQuery, GetReportsRequiringGradingResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetReportsRequiringGradingQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetReportsRequiringGradingResponse> Handle(GetReportsRequiringGradingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = _currentUserService.Role;

            // Only Lecturer can access
            if (userRole != RoleConstants.Lecturer)
            {
                return new GetReportsRequiringGradingResponse
                {
                    Success = false,
                    Message = "Access denied. Only lecturers can view reports requiring grading."
                };
            }

            // Get reports requiring grading
            var reports = await _unitOfWork.Reports.GetReportsRequiringGradingAsync(request.CourseId, request.AssignmentId, cancellationToken);

            var totalCount = reports.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination
            var paginatedReports = reports
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ReportDto
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
                    FileUrl = r.FileUrl,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

            return new GetReportsRequiringGradingResponse
            {
                Success = true,
                Message = "Reports requiring grading retrieved successfully",
                Reports = paginatedReports,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };
        }
        catch (Exception ex)
        {
            return new GetReportsRequiringGradingResponse
            {
                Success = false,
                Message = $"Error retrieving reports requiring grading: {ex.Message}"
            };
        }
    }
}
