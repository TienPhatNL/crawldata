using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetMyReportsQueryHandler : IRequestHandler<GetMyReportsQuery, GetMyReportsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyReportsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyReportsResponse> Handle(GetMyReportsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new GetMyReportsResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            // Get reports submitted by the current user
            var myReports = await _unitOfWork.Reports.GetReportsByStudentAsync(currentUserId.Value, cancellationToken);
            
            // Also get group reports where the user is CURRENTLY a member (not historical)
            // First get user's enrollments
            var userEnrollments = await _unitOfWork.CourseEnrollments.GetManyAsync(
                e => e.StudentId == currentUserId.Value,
                cancellationToken);
            
            var userEnrollmentIds = userEnrollments.Select(e => e.Id).ToList();
            
            // Get user's CURRENT group memberships (active groups only)
            var userGroupMembers = await _unitOfWork.GroupMembers.GetManyAsync(
                gm => userEnrollmentIds.Contains(gm.EnrollmentId),
                cancellationToken);
            
            var currentGroupIds = userGroupMembers.Select(gm => gm.GroupId).Distinct().ToList();
            
            // Get group reports ONLY for groups where user is CURRENTLY a member
            var groupReports = await _unitOfWork.Reports.GetManyAsync(
                r => r.GroupId.HasValue && currentGroupIds.Contains(r.GroupId.Value),
                cancellationToken,
                r => r.Assignment,
                r => r.Assignment.Course!,
                r => r.Group!);
            
            // Combine individual and group reports (avoid duplicates)
            var allReports = myReports
                .Union(groupReports)
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Apply filters
            if (request.CourseId.HasValue)
            {
                allReports = allReports.Where(r => r.Assignment.CourseId == request.CourseId.Value).ToList();
            }

            if (request.AssignmentId.HasValue)
            {
                allReports = allReports.Where(r => r.AssignmentId == request.AssignmentId.Value).ToList();
            }

            if (request.Status.HasValue)
            {
                allReports = allReports.Where(r => r.Status == request.Status.Value).ToList();
            }

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(request.CourseName))
            {
                var searchLower = request.CourseName.ToLower();
                allReports = allReports.Where(r => r.Assignment.Course != null && 
                    r.Assignment.Course.Name.ToLower().Contains(searchLower)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.AssignmentName))
            {
                var searchLower = request.AssignmentName.ToLower();
                allReports = allReports.Where(r => r.Assignment.Title.ToLower().Contains(searchLower)).ToList();
            }

            var totalCount = allReports.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination
            var paginatedReports = allReports
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ReportDto
                {
                    Id = r.Id,
                    AssignmentId = r.AssignmentId,
                    AssignmentTitle = r.Assignment.Title,
                    CourseId = r.Assignment.CourseId,
                    CourseName = r.Assignment.Course?.Name ?? "Unknown Course",
                    GroupId = r.GroupId,
                    GroupName = r.Group?.Name,
                    SubmittedBy = r.SubmittedBy,
                    SubmittedAt = r.SubmittedAt,
                    Status = r.Status,
                    Grade = r.Grade,
                    Feedback = r.Feedback,
                    GradedBy = r.GradedBy,
                    GradedAt = r.GradedAt,
                    IsGroupSubmission = r.IsGroupSubmission,
                    Version = r.Version,
                    FileUrl = r.FileUrl,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

            return new GetMyReportsResponse
            {
                Success = true,
                Message = "Your reports retrieved successfully",
                Reports = paginatedReports,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber
            };
        }
        catch (Exception ex)
        {
            return new GetMyReportsResponse
            {
                Success = false,
                Message = $"Error retrieving your reports: {ex.Message}"
            };
        }
    }
}
