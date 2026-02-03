using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Application.Features.Reports.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Queries;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, GetReportByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetReportByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetReportByIdResponse> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new GetReportByIdResponse { Success = false, Message = "User not authenticated" };
            }

            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new GetReportByIdResponse { Success = false, Message = "Report not found" };
            }

            // Authorization check: User can only view report if they are:
            // 1. The lecturer of the course
            // 2. The submitter (for individual reports)
            // 3. Currently a member of the group (for group reports)
            
            var isLecturer = report.Assignment?.Course?.LecturerId == currentUserId.Value;
            var isSubmitter = report.SubmittedBy == currentUserId.Value;
            
            bool isAuthorized = isLecturer || isSubmitter;
            
            // For group reports, check if user is CURRENTLY a group member
            if (!isAuthorized && report.IsGroupSubmission && report.GroupId.HasValue)
            {
                // Get user's enrollments
                var userEnrollments = await _unitOfWork.CourseEnrollments.GetManyAsync(
                    e => e.StudentId == currentUserId.Value,
                    cancellationToken);
                
                var userEnrollmentIds = userEnrollments.Select(e => e.Id).ToList();
                
                // Check if user is CURRENTLY a member of this group
                var isCurrentMember = await _unitOfWork.GroupMembers.ExistsAsync(
                    gm => gm.GroupId == report.GroupId.Value && userEnrollmentIds.Contains(gm.EnrollmentId),
                    cancellationToken);
                
                isAuthorized = isCurrentMember;
            }
            
            if (!isAuthorized)
            {
                return new GetReportByIdResponse { Success = false, Message = "You do not have permission to view this report" };
            }

            var dto = new ReportDetailDto
            {
                Id = report.Id,
                AssignmentId = report.AssignmentId,
                AssignmentTitle = report.Assignment?.Title ?? string.Empty,
                AssignmentDescription = report.Assignment?.Description ?? string.Empty,
                AssignmentMaxPoints = report.Assignment?.MaxPoints,
                AssignmentDueDate = report.Assignment?.DueDate ?? DateTime.MinValue,
                CourseCode = report.Assignment?.Course?.CourseCode?.Code ?? string.Empty,
                CourseName = report.Assignment?.Course?.Name ?? string.Empty,
                GroupId = report.GroupId,
                GroupName = report.Group?.Name,
                SubmittedBy = report.SubmittedBy,
                SubmittedAt = report.SubmittedAt,
                Submission = report.Submission,
                Status = report.Status,
                Grade = report.Grade,
                Feedback = report.Feedback,
                GradedBy = report.GradedBy,
                GradedAt = report.GradedAt,
                IsGroupSubmission = report.IsGroupSubmission,
                Version = report.Version,
                FileUrl = report.FileUrl,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };

            return new GetReportByIdResponse { Success = true, Message = "Report retrieved successfully", Report = dto };
        }
        catch (Exception ex)
        {
            return new GetReportByIdResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
