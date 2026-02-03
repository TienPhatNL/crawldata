using System.Security.Claims;
using System.Text.Json;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Events;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Handler for reverting report content to a previous historical version
/// Restores Submission (text content) and FileUrl (file attachment) without changing status
/// Only the original submitter or group leader can revert content
/// </summary>
public class RevertContentReportCommandHandler : IRequestHandler<RevertContentReportCommand, RevertContentReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ReportHistoryService _historyService;
    private readonly IPublisher _publisher;
    private readonly IKafkaUserService _userService;
    private readonly ILogger<RevertContentReportCommandHandler> _logger;

    public RevertContentReportCommandHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ReportHistoryService historyService,
        IPublisher publisher,
        IKafkaUserService userService,
        ILogger<RevertContentReportCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _historyService = historyService;
        _publisher = publisher;
        _userService = userService;
        _logger = logger;
    }

    public async Task<RevertContentReportResponse> Handle(RevertContentReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new RevertContentReportResponse 
                { 
                    Success = false, 
                    Message = "Report not found" 
                };
            }

            var userId = Guid.Parse(_httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Check authorization - only original submitter or group leader
            if (report.GroupId.HasValue)
            {
                // For group reports, check if user is the group leader
                var groupLeader = await _unitOfWork.GroupMembers.GetGroupLeaderAsync(
                    report.GroupId.Value, 
                    cancellationToken);
                
                if (groupLeader == null)
                {
                    return new RevertContentReportResponse 
                    { 
                        Success = false, 
                        Message = "Group leader not found" 
                    };
                }

                // Get enrollment to check if it's the current user
                var enrollment = await _unitOfWork.CourseEnrollments.GetByIdAsync(groupLeader.EnrollmentId, cancellationToken);
                if (enrollment == null || enrollment.StudentId != userId)
                {
                    return new RevertContentReportResponse 
                    { 
                        Success = false, 
                        Message = "Only the group leader can revert report content" 
                    };
                }
            }
            else
            {
                // For individual reports, only the original submitter can revert
                if (report.SubmittedBy != userId)
                {
                    return new RevertContentReportResponse 
                    { 
                        Success = false, 
                        Message = "You can only revert content for reports you submitted" 
                    };
                }
            }

            // Fetch historical version from history
            var historicalVersion = await _unitOfWork.ReportHistory.GetVersionAsync(
                report.Id, 
                request.Version, 
                cancellationToken);

            if (historicalVersion == null)
            {
                return new RevertContentReportResponse 
                { 
                    Success = false, 
                    Message = $"Version {request.Version} not found in report history" 
                };
            }

            // Parse historical content from NewValues
            if (string.IsNullOrWhiteSpace(historicalVersion.NewValues))
            {
                return new RevertContentReportResponse 
                { 
                    Success = false, 
                    Message = "Historical version does not contain content data" 
                };
            }

            var historicalData = JsonSerializer.Deserialize<Dictionary<string, object>>(historicalVersion.NewValues);
            if (historicalData == null)
            {
                return new RevertContentReportResponse 
                { 
                    Success = false, 
                    Message = "Failed to parse historical version data" 
                };
            }

            // Store current values for history tracking (including status to preserve it)
            var oldValues = new Dictionary<string, object>
            {
                ["Submission"] = report.Submission ?? "",
                ["FileUrl"] = report.FileUrl ?? "",
                ["Status"] = report.Status.ToString()
            };

            // Restore content fields from historical version
            if (historicalData.TryGetValue("Submission", out var submission))
            {
                report.Submission = submission?.ToString() ?? "";
            }

            if (historicalData.TryGetValue("FilePath", out var filePath))
            {
                report.FileUrl = filePath?.ToString() ?? "";
            }
            else if (historicalData.TryGetValue("FileUrl", out var fileUrl))
            {
                report.FileUrl = fileUrl?.ToString() ?? "";
            }

            // Increment version for the revert action (creates new version in history)
            report.Version++;
            report.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track content revert in history with new version number
            // Include Status in newValues to maintain it in history
            var newValues = new Dictionary<string, object>
            {
                ["Submission"] = report.Submission ?? "",
                ["FileUrl"] = report.FileUrl ?? "",
                ["Status"] = report.Status.ToString(),
                ["RevertedToVersion"] = request.Version.ToString()
            };

            await _historyService.TrackChangeAsync(
                reportId: report.Id,
                action: ReportHistoryAction.ContentReverted,
                changedBy: userId.ToString(),
                version: report.Version, // Use new incremented version
                oldValues: oldValues,
                newValues: newValues,
                comment: string.IsNullOrWhiteSpace(request.Comment) 
                    ? $"Content reverted to version {request.Version}"
                    : request.Comment,
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fetch contributor information and update history with name
            var contributorInfo = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";
            
            // Get all history records and find the most recent one
            var allHistory = await _unitOfWork.ReportHistory.GetReportHistoryAsync(report.Id, cancellationToken);
            var historyRecord = allHistory.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
            
            if (historyRecord != null)
            {
                historyRecord.Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? $"Content reverted to version {request.Version} | Contributors: {contributorName}"
                    : $"{request.Comment} | Contributors: {contributorName}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Report {ReportId} content reverted to version {Version} by User {UserId}",
                report.Id,
                request.Version,
                userId);

            // Raise event for group reports to notify all members
            if (report.GroupId.HasValue)
            {
                var assignment = await _unitOfWork.Assignments.GetByIdAsync(report.AssignmentId, cancellationToken);
                var group = await _unitOfWork.Groups.GetByIdAsync(report.GroupId.Value, cancellationToken);
                var groupMembers = await _unitOfWork.GroupMembers.GetMembersByGroupAsync(report.GroupId.Value, cancellationToken);
                
                // Get all member student IDs
                var memberEnrollments = new List<Guid>();
                foreach (var member in groupMembers)
                {
                    var enrollment = await _unitOfWork.CourseEnrollments.GetByIdAsync(member.EnrollmentId, cancellationToken);
                    if (enrollment != null)
                    {
                        memberEnrollments.Add(enrollment.StudentId);
                    }
                }

                // Get user name
                var userClaim = _httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.Name);
                var userName = userClaim?.Value ?? "Unknown";

                _logger.LogInformation(
                    "[RevertContentReport] Creating event for group {GroupId} with {MemberCount} members. Members: {MemberIds}",
                    report.GroupId.Value,
                    memberEnrollments.Count,
                    string.Join(", ", memberEnrollments));

                var revertEvent = new ReportContentRevertedEvent(
                    reportId: report.Id,
                    courseId: assignment?.CourseId ?? Guid.Empty,
                    assignmentId: report.AssignmentId,
                    assignmentTitle: assignment?.Title ?? "Unknown",
                    groupId: report.GroupId.Value,
                    groupName: group?.Name ?? "Unknown",
                    revertedBy: userId,
                    revertedByName: userName,
                    revertedToVersion: request.Version,
                    groupMemberIds: memberEnrollments,
                    comment: request.Comment
                );

                _logger.LogInformation(
                    "[RevertContentReport] Publishing ReportContentRevertedEvent to MediatR for report {ReportId}",
                    report.Id);

                await _publisher.Publish(revertEvent, cancellationToken);
                
                _logger.LogInformation(
                    "[RevertContentReport] Successfully published ReportContentRevertedEvent");
            }

            return new RevertContentReportResponse
            {
                Success = true,
                Message = $"Report content successfully reverted to version {request.Version}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting report {ReportId} to version {Version}", request.ReportId, request.Version);
            return new RevertContentReportResponse
            {
                Success = false,
                Message = $"An error occurred while reverting the report content: {ex.Message}"
            };
        }
    }
}
