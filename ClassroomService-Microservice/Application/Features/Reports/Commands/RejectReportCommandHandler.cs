using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class RejectReportCommandHandler : IRequestHandler<RejectReportCommand, RejectReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public RejectReportCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ReportHistoryService historyService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _historyService = historyService;
    }

    public async Task<RejectReportResponse> Handle(RejectReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new RejectReportResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Get the report with details
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new RejectReportResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Only Submitted, Resubmitted, or Late reports can be rejected
            if (report.Status != ReportStatus.Submitted && 
                report.Status != ReportStatus.Resubmitted &&
                report.Status != ReportStatus.Late)
            {
                return new RejectReportResponse
                {
                    Success = false,
                    Message = $"Cannot reject report with status: {report.Status}. Only submitted, resubmitted, or late reports can be rejected."
                };
            }

            // Verify lecturer has access to this course
            var assignment = report.Assignment;
            if (assignment == null)
            {
                return new RejectReportResponse
                {
                    Success = false,
                    Message = "Assignment not found"
                };
            }

            var course = await _unitOfWork.Courses.GetAsync(c => c.Id == assignment.CourseId, cancellationToken);
            if (course == null || course.LecturerId != userId)
            {
                return new RejectReportResponse
                {
                    Success = false,
                    Message = "You don't have permission to reject this report"
                };
            }

            // Reject the report (FINAL rejection, not requiring revision)
            var oldStatus = report.Status.ToString();
            var now = DateTime.UtcNow;
            report.Status = ReportStatus.Rejected;
            report.Feedback = request.Feedback;
            report.GradedBy = userId;
            report.GradedAt = now;
            report.UpdatedAt = now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track rejection in history (final rejection, not requiring revision)
            await _historyService.TrackRejectionAsync(
                report.Id,
                userId.ToString(),
                report.Version,
                request.Feedback ?? string.Empty,
                oldStatus,
                cancellationToken);
            
            // Save the history record
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fetch contributor information
            var contributorInfo = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";
            var currentUserRole = _currentUserService.Role;
            
            // Update history record with contributor name
            var historyRecord = await _unitOfWork.ReportHistory.GetVersionAsync(report.Id, report.Version, cancellationToken);
            if (historyRecord != null)
            {
                historyRecord.Comment = $"Report rejected by lecturer (final rejection) | Contributors: {contributorName}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new RejectReportResponse
            {
                Success = true,
                Message = "Report rejected successfully. Student can now revise and resubmit.",
                ContributorId = userId,
                ContributorName = contributorInfo?.FullName ?? "Unknown",
                ContributorRole = currentUserRole ?? "Lecturer"
            };
        }
        catch (Exception ex)
        {
            return new RejectReportResponse
            {
                Success = false,
                Message = $"An error occurred while rejecting the report: {ex.Message}"
            };
        }
    }
}
