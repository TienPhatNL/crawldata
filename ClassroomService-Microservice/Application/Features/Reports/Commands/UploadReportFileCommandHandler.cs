using ClassroomService.Application.Common.Helpers;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Application.Features.Reports.Commands;

/// <summary>
/// Handler for uploading report file attachments
/// Tracks file changes with versioning in ReportHistory
/// </summary>
public class UploadReportFileCommandHandler : IRequestHandler<UploadReportFileCommand, UploadReportFileResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;
    private readonly ReportHistoryService _historyService;
    private readonly ILogger<UploadReportFileCommandHandler> _logger;

    public UploadReportFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUploadService uploadService,
        ReportHistoryService historyService,
        ILogger<UploadReportFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<UploadReportFileResponse> Handle(UploadReportFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user is authenticated
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return new UploadReportFileResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var userId = currentUserId.Value;

            // Validate file
            if (!FileValidationHelper.ValidateReportFile(request.File, out var validationError))
            {
                return new UploadReportFileResponse
                {
                    Success = false,
                    Message = validationError
                };
            }

            // Get report
            var report = await _unitOfWork.Reports.GetAsync(
                r => r.Id == request.ReportId,
                cancellationToken);

            if (report == null)
            {
                return new UploadReportFileResponse
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
                        return new UploadReportFileResponse
                        {
                            Success = false,
                            Message = "You do not have permission to upload files for this report"
                        };
                    }
                }
                else
                {
                    return new UploadReportFileResponse
                    {
                        Success = false,
                        Message = "You do not have permission to upload files for this report"
                    };
                }
            }

            // Check report status - only allow uploads in Draft or RequiresRevision
            if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RequiresRevision)
            {
                return new UploadReportFileResponse
                {
                    Success = false,
                    Message = $"Files can only be uploaded when report is in Draft or RequiresRevision status. Current status: {report.Status}"
                };
            }

            // Store old file URL for tracking
            var oldFileUrl = report.FileUrl;

            // Upload new file
            var fileUrl = await _uploadService.UploadFileAsync(request.File);
            
            // Update report - DO NOT delete old file (keep for versioning)
            report.FileUrl = fileUrl;
            await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track the file upload change in ReportHistory
            await _historyService.TrackFileUploadAsync(
                reportId: report.Id,
                uploadedBy: userId.ToString(),
                version: report.Version,
                oldFileUrl: oldFileUrl,
                newFileUrl: fileUrl,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Report file uploaded successfully for Report {ReportId} by User {UserId}. File: {FileName}",
                request.ReportId,
                userId,
                request.File.FileName);

            return new UploadReportFileResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FileUrl = fileUrl,
                Version = report.Version
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for Report {ReportId}", request.ReportId);
            return new UploadReportFileResponse
            {
                Success = false,
                Message = $"An error occurred while uploading the file: {ex.Message}"
            };
        }
    }
}
