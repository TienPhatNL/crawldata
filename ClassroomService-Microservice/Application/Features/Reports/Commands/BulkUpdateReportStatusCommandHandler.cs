using MediatR;
using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Constants;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;

namespace ClassroomService.Application.Features.Reports.Commands;

public class BulkUpdateReportStatusCommandHandler : IRequestHandler<BulkUpdateReportStatusCommand, BulkUpdateReportStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public BulkUpdateReportStatusCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<BulkUpdateReportStatusResponse> Handle(BulkUpdateReportStatusCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var updatedCount = 0;

        try
        {
            var userRole = _currentUserService.Role;

            // Only Lecturer can access
            if (userRole != RoleConstants.Lecturer)
            {
                return new BulkUpdateReportStatusResponse
                {
                    Success = false,
                    Message = "Access denied. Only lecturers can update report statuses."
                };
            }

            // Get all reports by IDs
            var reports = await _unitOfWork.Reports.GetReportsByIdsAsync(request.ReportIds, cancellationToken);
            var foundReportIds = reports.Select(r => r.Id).ToList();

            // Check for missing reports
            var missingIds = request.ReportIds.Except(foundReportIds).ToList();
            if (missingIds.Any())
            {
                errors.Add($"Reports not found: {string.Join(", ", missingIds)}");
            }

            // Update each report
            foreach (var report in reports)
            {
                // Only update if status is Submitted or Resubmitted
                if (report.Status == ReportStatus.Submitted || report.Status == ReportStatus.Resubmitted)
                {
                    report.Status = ReportStatus.UnderReview;
                    await _unitOfWork.Reports.UpdateAsync(report, cancellationToken);
                    updatedCount++;
                }
                else
                {
                    errors.Add($"Report {report.Id} cannot be updated (current status: {report.Status})");
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new BulkUpdateReportStatusResponse
            {
                Success = updatedCount > 0,
                Message = updatedCount > 0 
                    ? $"Successfully updated {updatedCount} report(s) to UnderReview status" 
                    : "No reports were updated",
                UpdatedCount = updatedCount,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new BulkUpdateReportStatusResponse
            {
                Success = false,
                Message = $"Error updating reports: {ex.Message}",
                UpdatedCount = updatedCount,
                Errors = errors
            };
        }
    }
}
