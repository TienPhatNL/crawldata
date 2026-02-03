using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Handler for deleting report file attachments
/// Tracks the deletion in ReportHistory
/// </summary>
public class DeleteReportFileCommandHandler : IRequestHandler<DeleteReportFileCommand, DeleteReportFileResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ReportHistoryService _historyService;
    private readonly ILogger<DeleteReportFileCommandHandler> _logger;

    public DeleteReportFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ReportHistoryService historyService,
        ILogger<DeleteReportFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<DeleteReportFileResponse> Handle(DeleteReportFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new DeleteReportFileResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Get report
            var report = await _unitOfWork.Reports.GetAsync(
                r => r.Id == request.ReportId,
                cancellationToken);

            if (report == null)
            {
                return new DeleteReportFileResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Check if user owns the report
            if (report.SubmittedBy != userId)
            {
                // For group submissions, check if user is a group member
                if (report.IsGroupSubmission && report.GroupId.HasValue)
                {
                    var isMember = await _unitOfWork.GroupMembers.ExistsAsync(
                        gm => gm.GroupId == report.GroupId.Value && gm.StudentId == userId,
                        cancellationToken);

                    if (!isMember)
                    {
                        return new DeleteReportFileResponse
                        {
                            Success = false,
                            Message = "You do not have permission to delete files for this report"
                        };
                    }
                }
                else
                {
                    return new DeleteReportFileResponse
                    {
                        Success = false,
                        Message = "You do not have permission to delete files for this report"
                    };
                }
            }

            // Check report status - only allow deletion in Draft or RequiresRevision
            if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RequiresRevision)
            {
                return new DeleteReportFileResponse
                {
                    Success = false,
                    Message = $"Files can only be deleted when report is in Draft or RequiresRevision status. Current status: {report.Status}"
                };
            }

            // Check if there's a file to delete
            if (string.IsNullOrEmpty(report.FileUrl))
            {
                return new DeleteReportFileResponse
                {
                    Success = false,
                    Message = "Report does not have a file to delete"
                };
            }

            var oldFileUrl = report.FileUrl;

            // Delete from S3
            try
            {
                await _uploadService.DeleteFileAsync(report.FileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete report file from S3: {FileUrl}", report.FileUrl);
                return new DeleteReportFileResponse
                {
                    Success = false,
                    Message = "Failed to delete file from storage"
                };
            }

            // Update report
            report.FileUrl = null;
            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track the file deletion in ReportHistory
            await _historyService.TrackFileUploadAsync(
                reportId: report.Id,
                uploadedBy: userId.ToString(),
                version: report.Version,
                oldFileUrl: oldFileUrl,
                newFileUrl: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Report file deleted successfully for Report {ReportId} by User {UserId}",
                request.ReportId,
                userId);

            return new DeleteReportFileResponse
            {
                Success = true,
                Message = "File deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file for Report {ReportId}", request.ReportId);
            return new DeleteReportFileResponse
            {
                Success = false,
                Message = $"An error occurred while deleting the file: {ex.Message}"
            };
        }
    }
}
