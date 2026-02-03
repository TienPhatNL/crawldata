using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class ResubmitReportCommandHandler : IRequestHandler<ResubmitReportCommand, ResubmitReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;

    public ResubmitReportCommandHandler(
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

    public async Task<ResubmitReportResponse> Handle(ResubmitReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new ResubmitReportResponse { Success = false, Message = "User not authenticated" };
            }

            var userId = currentUserId.Value;

            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new ResubmitReportResponse { Success = false, Message = "Report not found" };
            }

            if (report.Status != ReportStatus.RequiresRevision)
            {
                return new ResubmitReportResponse { Success = false, Message = "Only reports requiring revision can be resubmitted" };
            }

            // Authorization check: For group submissions, only group leader can resubmit
            // For individual submissions, only the original submitter can resubmit
            if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value, cancellationToken);
                if (group == null)
                {
                    return new ResubmitReportResponse { Success = false, Message = "Group not found" };
                }

                // Find group leader
                var groupLeader = group.Members.FirstOrDefault(m => m.IsLeader);
                if (groupLeader == null)
                {
                    return new ResubmitReportResponse { Success = false, Message = "Group has no leader assigned" };
                }

                // Check if current user is the leader
                if (groupLeader.StudentId == Guid.Empty || groupLeader.StudentId != userId)
                {
                    return new ResubmitReportResponse { Success = false, Message = "Only the group leader can resubmit the report" };
                }
            }
            else
            {
                // Individual assignment - only the original submitter can resubmit
                if (report.SubmittedBy != userId)
                {
                    return new ResubmitReportResponse { Success = false, Message = "You can only resubmit reports you submitted" };
                }
            }

            report.Status = ReportStatus.Resubmitted; // Set to Resubmitted
            // NOTE: Version is NOT incremented - only content updates increment version
            // This resubmission will create a new sequence within the current version
            report.SubmittedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            // Get submitter name and lecturer ID for event
            var submitter = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var submitterName = submitter?.FullName ?? "Unknown Student";
            var lecturerId = report.Assignment?.Course?.LecturerId ?? Guid.Empty;

            // Raise domain event
            report.AddDomainEvent(new ReportResubmittedEvent(
                report.Id,
                report.AssignmentId,
                report.Assignment?.Title ?? "Unknown Assignment",
                report.Assignment?.CourseId ?? Guid.Empty,
                userId,
                submitterName,
                report.Version,
                report.GroupId,
                lecturerId));

            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track resubmission in history
            await _historyService.TrackResubmissionAsync(
                report.Id,
                userId.ToString(),
                report.Version,
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
                historyRecord.Comment = $"Resubmitted after revision | Contributors: {contributorName}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new ResubmitReportResponse 
            { 
                Success = true, 
                Message = "Report resubmitted successfully",
                ContributorId = userId,
                ContributorName = contributorInfo?.FullName ?? "Unknown",
                ContributorRole = currentUserRole ?? "Student"
            };
        }
        catch (Exception ex)
        {
            return new ResubmitReportResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
