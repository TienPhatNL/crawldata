using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Reports.Commands;

public class UpdateReportCommandHandler : IRequestHandler<UpdateReportCommand, UpdateReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IKafkaUserService _userService;
    private readonly ReportHistoryService _historyService;
    private readonly IChangeTrackingService _changeTrackingService;
    private readonly ILogger<UpdateReportCommandHandler> _logger;

    public UpdateReportCommandHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        IKafkaUserService userService,
        ReportHistoryService historyService,
        IChangeTrackingService changeTrackingService,
        ILogger<UpdateReportCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userService = userService;
        _historyService = historyService;
        _changeTrackingService = changeTrackingService;
        _logger = logger;
    }

    public async Task<UpdateReportResponse> Handle(UpdateReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UpdateReportResponse { Success = false, Message = "User not authenticated" };
            }

            var userId = currentUserId.Value;

            // Get report with details
            var report = await _unitOfWork.Reports.GetReportWithDetailsAsync(request.ReportId, cancellationToken);
            if (report == null)
            {
                return new UpdateReportResponse { Success = false, Message = "Report not found" };
            }

            // Only allow updates for Draft or RequiresRevision status
            if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RequiresRevision)
            {
                return new UpdateReportResponse
                {
                    Success = false,
                    Message = "Only Draft or reports requiring revision can be updated"
                };
            }

            // Enforce assignment type flow: group assignments must use collaboration hub
            if (report.IsGroupSubmission)
            {
                return new UpdateReportResponse
                {
                    Success = false,
                    Message = "Group assignments must use the real-time collaboration hub, not the update API"
                };
            }

            // For individual reports: only the submitter can edit
            if (report.SubmittedBy != userId)
            {
                return new UpdateReportResponse
                {
                    Success = false,
                    Message = "You can only edit your own reports"
                };
            }

            // Capture old values before update
            var oldContent = report.Submission ?? string.Empty;
            var newContent = request.Submission ?? string.Empty;
            var status = report.Status.ToString();

            // Nếu không có thay đổi nội dung thì không update, không tăng version
            if (string.Equals(oldContent, newContent, StringComparison.Ordinal))
            {
                _logger.LogInformation("No changes detected for report {ReportId}. Skipping update and version increment.", report.Id);

                return new UpdateReportResponse
                {
                    Success = true,
                    Message = $"No changes detected. Report remains at Version {report.Version}"
                };
            }

            // Calculate diff using change tracking service (chỉ khi có thay đổi)
            var diffResult = _changeTrackingService.CalculateDiff(oldContent, newContent);
            var changeSummary = _changeTrackingService.GenerateSummary(diffResult);
            var unifiedDiff = _changeTrackingService.CreateUnifiedDiff(oldContent, newContent);
            var changeDetails = _changeTrackingService.SerializeChangeOperations(diffResult);

            _logger.LogInformation("📊 Individual update - Change tracking: {ChangeSummary}", changeSummary);

            // Update report và CHỈ TĂNG VERSION khi có thay đổi
            report.Submission = newContent;
            report.Version++; 
            report.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track the update in history with change tracking data
            await _historyService.TrackUpdateAsync(
                report.Id,
                userId.ToString(),
                report.Version,
                oldContent,
                newContent,
                null, 
                null, 
                status,
                cancellationToken);

            // Save to commit the history record first
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get user info for contributor name
            var contributorInfo = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var contributorName = contributorInfo?.FullName ?? "Unknown";

            // Now retrieve and update the history record with change tracking details
            var historyRecord = await _unitOfWork.ReportHistory.GetVersionAsync(report.Id, report.Version, cancellationToken);

            if (historyRecord != null)
            {
                historyRecord.ChangeSummary = changeSummary;
                historyRecord.ChangeDetails = changeDetails;
                historyRecord.UnifiedDiff = unifiedDiff;
                historyRecord.Comment = $"Individual update | Contributors: {contributorName}";

                await _unitOfWork.ReportHistory.UpdateAsync(historyRecord, cancellationToken);
                _logger.LogInformation("📝 Updated history record for version {Version} with change tracking", report.Version);

                // Save the updated tracking data
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("⚠️ Could not find history record for version {Version} to add tracking data", report.Version);
            }

            return new UpdateReportResponse
            {
                Success = true,
                Message = $"Report updated successfully (Version {report.Version})"
            };
        }
        catch (Exception ex)
        {
            return new UpdateReportResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
