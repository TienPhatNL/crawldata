using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportsByAssignmentQueryHandler : IRequestHandler<GetReportsByAssignmentQuery, GetReportsByAssignmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;

    public GetReportsByAssignmentQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IKafkaUserService userService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<GetReportsByAssignmentResponse> Handle(GetReportsByAssignmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all reports for the assignment
            var allReports = await _unitOfWork.Reports.GetReportsByAssignmentAsync(request.AssignmentId, cancellationToken);

            // Role-based filtering
            var userRole = _currentUserService.Role;
            var currentUserId = _currentUserService.UserId;

            // If student, show their own submissions AND group submissions where they are a member
            if (userRole == RoleConstants.Student && currentUserId.HasValue)
            {
                // Get user's enrollments
                var userEnrollments = await _unitOfWork.CourseEnrollments.GetManyAsync(
                    e => e.StudentId == currentUserId.Value,
                    cancellationToken);
                
                var userEnrollmentIds = userEnrollments.Select(e => e.Id).ToList();
                
                // Get user's group memberships
                var userGroupMembers = await _unitOfWork.GroupMembers.GetManyAsync(
                    gm => userEnrollmentIds.Contains(gm.EnrollmentId),
                    cancellationToken);
                
                var userGroupIds = userGroupMembers.Select(gm => gm.GroupId).ToList();
                
                // Filter: reports submitted by the user OR reports for groups the user is in
                allReports = allReports.Where(r => 
                    r.SubmittedBy == currentUserId.Value || 
                    (r.GroupId.HasValue && userGroupIds.Contains(r.GroupId.Value))
                ).ToList();
            }

            // Apply status filter if provided
            if (request.Status.HasValue)
            {
                allReports = allReports.Where(r => r.Status == request.Status.Value).ToList();
            }

            var totalCount = allReports.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination
            var paginatedReports = allReports
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

            return new GetReportsByAssignmentResponse
            {
                Success = true,
                Message = "Reports retrieved successfully",
                Reports = reportDtos,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };
        }
        catch (Exception ex)
        {
            return new GetReportsByAssignmentResponse
            {
                Success = false,
                Message = $"Error retrieving reports: {ex.Message}"
            };
        }
    }
}
