using MediatR;
using Microsoft.Extensions.Logging;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Constants;
using ClassroomService.Application.Common.Interfaces;
using System.Text.Json;

namespace ClassroomService.Application.Features.Reports.Commands;

public class UpdateReportStatusCommandHandler : IRequestHandler<UpdateReportStatusCommand, UpdateReportStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<UpdateReportStatusCommandHandler> _logger;

    public UpdateReportStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ILogger<UpdateReportStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<UpdateReportStatusResponse> Handle(UpdateReportStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            var currentUserRole = _currentUserService.Role;

            if (!currentUserId.HasValue || currentUserRole != RoleConstants.Student)
            {
                return new UpdateReportStatusResponse
                {
                    Success = false,
                    Message = "Only students can update report status"
                };
            }

            // Validate target status (only Draft or RequiresRevision allowed)
            if (request.TargetStatus != ReportStatus.Draft && 
                request.TargetStatus != ReportStatus.RequiresRevision)
            {
                return new UpdateReportStatusResponse
                {
                    Success = false,
                    Message = "Invalid target status. Only Draft or RequiresRevision are allowed"
                };
            }

            // Get report with all necessary relationships
            var report = await _unitOfWork.Reports.GetAsync(
                r => r.Id == request.ReportId,
                cancellationToken,
                r => r.Assignment,
                r => r.Assignment.Course,
                r => r.Group);

            if (report == null)
            {
                return new UpdateReportStatusResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Check assignment status (must be Active or Extended)
            if (report.Assignment?.Status != AssignmentStatus.Active && 
                report.Assignment?.Status != AssignmentStatus.Extended)
            {
                return new UpdateReportStatusResponse
                {
                    Success = false,
                    Message = $"Assignment must be Active or Extended. Current status: {report.Assignment?.Status}"
                };
            }

            // Check assignment due date - prevent updates if passed
            var effectiveDueDate = report.Assignment.ExtendedDueDate ?? report.Assignment.DueDate;
            var timeUntilDue = effectiveDueDate - DateTime.UtcNow;

            if (timeUntilDue.TotalDays <= 0)
            {
                return new UpdateReportStatusResponse
                {
                    Success = false,
                    Message = $"Cannot update report status after due date ({effectiveDueDate:yyyy-MM-dd HH:mm} UTC)"
                };
            }

            // Validate current report status based on target
            if (request.TargetStatus == ReportStatus.Draft)
            {
                // Can move to Draft only from Submitted
                if (report.Status != ReportStatus.Submitted)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = $"Can only revert to Draft from Submitted status. Current status: {report.Status}"
                    };
                }
            }
            else if (request.TargetStatus == ReportStatus.RequiresRevision)
            {
                // Can move to RequiresRevision only from Resubmitted
                if (report.Status != ReportStatus.Resubmitted)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = $"Can only revert to RequiresRevision from Resubmitted status. Current status: {report.Status}"
                    };
                }
            }

            // Authorization check
            bool isAuthorized = false;
            
            if (report.IsGroupSubmission && report.GroupId.HasValue)
            {
                // For group reports: must be the group leader
                var group = await _unitOfWork.Groups.GetGroupWithMembersAsync(report.GroupId.Value, cancellationToken);
                
                if (group == null)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = "Group not found"
                    };
                }

                // Find current user's enrollment
                var userEnrollment = await _unitOfWork.CourseEnrollments.GetAsync(
                    e => e.StudentId == currentUserId.Value && e.CourseId == report.Assignment.CourseId,
                    cancellationToken);

                if (userEnrollment == null)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = "You are not enrolled in this course"
                    };
                }

                // Check if user is a member by comparing student IDs directly
                var groupMember = group.Members?.FirstOrDefault(m => m.Enrollment.StudentId == currentUserId.Value);
                
                if (groupMember == null)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = "You are not a member of this group"
                    };
                }

                if (!groupMember.IsLeader)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = "Only the group leader can update report status"
                    };
                }

                isAuthorized = true;
            }
            else
            {
                // For individual reports: must be the submitter
                isAuthorized = report.SubmittedBy == currentUserId.Value;
                
                if (!isAuthorized)
                {
                    return new UpdateReportStatusResponse
                    {
                        Success = false,
                        Message = "You can only update your own reports"
                    };
                }
            }

            var oldStatus = report.Status;
            
            // Update status
            report.Status = request.TargetStatus;
            
            // Update timestamp based on target status
            if (request.TargetStatus == ReportStatus.Resubmitted)
            {
                report.SubmittedAt = DateTime.UtcNow;
            }
            else if (request.TargetStatus == ReportStatus.Draft || 
                     request.TargetStatus == ReportStatus.RequiresRevision)
            {
                // Clear submission timestamp when reverting to Draft or RequiresRevision
                report.SubmittedAt = null;
            }

            // NOTE: Version is NOT incremented for status changes
            // Only content updates (UpdateReportCommand) increment version
            // This status change will create a new sequence within the current version

            // Create history record
            var historyAction = request.TargetStatus switch
            {
                ReportStatus.Draft => ReportHistoryAction.RevertedToDraft,
                ReportStatus.RequiresRevision => ReportHistoryAction.StatusChanged,
                _ => ReportHistoryAction.StatusChanged
            };

            var historyEntry = new Domain.Entities.ReportHistory
            {
                ReportId = report.Id,
                Action = historyAction,
                ChangedBy = currentUserId.Value.ToString(),
                ChangedAt = DateTime.UtcNow,
                Version = report.Version,
                FieldsChanged = JsonSerializer.Serialize(new[] { "Status" }),
                OldValues = JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
                NewValues = JsonSerializer.Serialize(new { Status = request.TargetStatus.ToString() }),
                Comment = request.Comment,
                ContributorIds = JsonSerializer.Serialize(new[] { currentUserId.Value.ToString() })
            };

            await _unitOfWork.ReportHistory.AddAsync(historyEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Report {ReportId} status updated from {OldStatus} to {NewStatus} by user {UserId}",
                report.Id, oldStatus, request.TargetStatus, currentUserId.Value);

            // Fetch contributor information
            var contributorInfo = await _userService.GetUserByIdAsync(currentUserId.Value, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";
            
            // Update history record with contributor name
            historyEntry.Comment = string.IsNullOrEmpty(request.Comment) 
                ? $"Contributors: {contributorName}"
                : $"{request.Comment} | Contributors: {contributorName}";
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateReportStatusResponse
            {
                Success = true,
                Message = $"Report status successfully updated to {request.TargetStatus}",
                NewStatus = request.TargetStatus,
                ContributorId = currentUserId.Value,
                ContributorName = contributorInfo?.FullName ?? "Unknown",
                ContributorRole = currentUserRole ?? "Student"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report status for report {ReportId}", request.ReportId);
            return new UpdateReportStatusResponse
            {
                Success = false,
                Message = $"Error updating report status: {ex.Message}"
            };
        }
    }
}
