using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsByCourseQueryHandler : IRequestHandler<GetReportsByCourseQuery, GetReportsByCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;

    public GetReportsByCourseQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<GetReportsByCourseResponse> Handle(GetReportsByCourseQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = _currentUserService.Role;

            // Only Lecturer can access
            if (userRole != RoleConstants.Lecturer)
            {
                return new GetReportsByCourseResponse
                {
                    Success = false,
                    Message = "Access denied. Only lecturers can view course reports."
                };
            }

            // Get all reports for the course
            var reports = await _unitOfWork.Reports.GetReportsByCourseAsync(request.CourseId, cancellationToken);

            // Apply filters
            if (request.Status.HasValue)
            {
                reports = reports.Where(r => r.Status == request.Status.Value).ToList();
            }

            if (request.FromDate.HasValue)
            {
                reports = reports.Where(r => r.SubmittedAt.HasValue && r.SubmittedAt.Value >= request.FromDate.Value).ToList();
            }

            if (request.ToDate.HasValue)
            {
                reports = reports.Where(r => r.SubmittedAt.HasValue && r.SubmittedAt.Value <= request.ToDate.Value).ToList();
            }

            var totalCount = reports.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination
            var paginatedReports = reports
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Collect unique user IDs (SubmittedBy and GradedBy)
            var submittedByIds = paginatedReports.Select(r => r.SubmittedBy).Distinct();
            var gradedByIds = paginatedReports.Where(r => r.GradedBy.HasValue).Select(r => r.GradedBy!.Value).Distinct();
            var allUserIds = submittedByIds.Union(gradedByIds);

            // Fetch user information
            var users = await _userService.GetUsersByIdsAsync(allUserIds, cancellationToken);
            var userDict = users?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<Guid, Domain.DTOs.UserDto>();

            // Map to DTOs with user names
            var reportDtos = paginatedReports.Select(r =>
            {
                var submittedByName = userDict.TryGetValue(r.SubmittedBy, out var submitter)
                    ? $"{submitter.LastName} {submitter.FirstName}".Trim()
                    : null;

                var gradedByName = r.GradedBy.HasValue && userDict.TryGetValue(r.GradedBy.Value, out var grader)
                    ? $"{grader.LastName} {grader.FirstName}".Trim()
                    : null;

                return new ReportDto
                {
                    Id = r.Id,
                    AssignmentId = r.AssignmentId,
                    AssignmentTitle = r.Assignment.Title,
                    GroupId = r.GroupId,
                    GroupName = r.Group?.Name,
                    SubmittedBy = r.SubmittedBy,
                    SubmittedByName = submittedByName,
                    SubmittedAt = r.SubmittedAt,
                    Status = r.Status,
                    Grade = r.Grade,
                    GradedBy = r.GradedBy,
                    GradedByName = gradedByName,
                    GradedAt = r.GradedAt,
                    IsGroupSubmission = r.IsGroupSubmission,
                    Version = r.Version,
                    FileUrl = r.FileUrl,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                };
            }).ToList();

            return new GetReportsByCourseResponse
            {
                Success = true,
                Message = "Course reports retrieved successfully",
                Reports = reportDtos,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };
        }
        catch (Exception ex)
        {
            return new GetReportsByCourseResponse
            {
                Success = false,
                Message = $"Error retrieving course reports: {ex.Message}"
            };
        }
    }
}
