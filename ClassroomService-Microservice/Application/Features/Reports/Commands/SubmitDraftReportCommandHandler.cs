using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class SubmitDraftReportCommandHandler : IRequestHandler<SubmitDraftReportCommand, SubmitDraftReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public SubmitDraftReportCommandHandler(
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

    public async Task<SubmitDraftReportResponse> Handle(SubmitDraftReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new SubmitDraftReportResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Get the report
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new SubmitDraftReportResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Only Draft reports can be submitted
            if (report.Status != ReportStatus.Draft)
            {
                return new SubmitDraftReportResponse
                {
                    Success = false,
                    Message = $"Only draft reports can be submitted. Current status: {report.Status}"
                };
            }

            // For group submissions: only group leader can submit
            // For individual submissions: only the original creator can submit
            if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value, cancellationToken);
                if (group == null)
                {
                    return new SubmitDraftReportResponse
                    {
                        Success = false,
                        Message = "Group not found"
                    };
                }

                // Find group leader
                var groupLeader = group.Members.FirstOrDefault(m => m.IsLeader);
                if (groupLeader == null)
                {
                    return new SubmitDraftReportResponse
                    {
                        Success = false,
                        Message = "Group has no leader assigned"
                    };
                }

                // Check if current user is the leader
                // Use the StudentId convenience property which accesses Enrollment.StudentId
                if (groupLeader.StudentId == Guid.Empty || groupLeader.StudentId != userId)
                {
                    return new SubmitDraftReportResponse
                    {
                        Success = false,
                        Message = "Only the group leader can submit the final report"
                    };
                }
            }
            else
            {
                // Individual assignment - only the original creator can submit
                if (report.SubmittedBy != userId)
                {
                    return new SubmitDraftReportResponse
                    {
                        Success = false,
                        Message = "You can only submit reports you created"
                    };
                }
            }

            // Check if assignment is still accepting submissions
            var assignment = report.Assignment;
            if (assignment == null)
            {
                return new SubmitDraftReportResponse
                {
                    Success = false,
                    Message = "Assignment not found"
                };
            }

            if (assignment.Status != AssignmentStatus.Active && assignment.Status != AssignmentStatus.Extended)
            {
                return new SubmitDraftReportResponse
                {
                    Success = false,
                    Message = "Assignment is not accepting submissions"
                };
            }

            // Check deadline and determine if late
            var now = DateTime.UtcNow;
            var dueDate = assignment.ExtendedDueDate ?? assignment.DueDate;
            var isLate = now > dueDate;

            // Update report status - set to Submitted or Late (not UnderReview)
            var newStatus = isLate ? ReportStatus.Late : ReportStatus.Submitted;
            report.Status = newStatus;
            report.SubmittedAt = now;
            report.UpdatedAt = now;

            // Get submitter name and group name for event
            var submitter = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var submitterName = submitter?.FullName ?? "Unknown Student";
            var groupName = report.IsGroupSubmission && report.Group != null ? report.Group.Name : null;

            // Raise domain event
            report.AddDomainEvent(new ReportSubmittedEvent(
                report.Id,
                report.AssignmentId,
                assignment.Title,
                assignment.CourseId,
                assignment.Course?.Name ?? "Unknown Course",
                userId,
                submitterName,
                report.IsGroupSubmission,
                report.GroupId,
                groupName,
                assignment.Course?.LecturerId ?? Guid.Empty));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track submission in history
            await _historyService.TrackSubmissionAsync(
                report.Id,
                userId.ToString(),
                report.Version,
                newStatus.ToString(),
                now,
                isLate,
                cancellationToken);
            
            // Save the history record
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Fetch contributor information and update history with name
            var contributorInfo = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";
            
            // Get all history records and find the most recent one
            var allHistory = await _unitOfWork.ReportHistory.GetReportHistoryAsync(report.Id, cancellationToken);
            var historyRecord = allHistory.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
            
            if (historyRecord != null)
            {
                historyRecord.Comment = string.IsNullOrEmpty(historyRecord.Comment) 
                    ? $"Contributors: {contributorName}"
                    : $"{historyRecord.Comment} | Contributors: {contributorName}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new SubmitDraftReportResponse
            {
                Success = true,
                Message = isLate 
                    ? "Report submitted but marked as late due to deadline" 
                    : "Report submitted successfully"
            };
        }
        catch (Exception ex)
        {
            return new SubmitDraftReportResponse
            {
                Success = false,
                Message = $"An error occurred while submitting the report: {ex.Message}"
            };
        }
    }
}
