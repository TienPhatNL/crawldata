using ClassroomService.Application.Common.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Reports.Commands;

public class DeleteReportCommandHandler : IRequestHandler<DeleteReportCommand, DeleteReportResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteReportCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteReportResponse> Handle(DeleteReportCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _unitOfWork.Reports.GetAsync(r => r.Id == request.ReportId, cancellationToken);
            if (report == null)
            {
                return new DeleteReportResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            // Only allow deletion for Draft and Submitted statuses
            if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.Submitted)
            {
                return new DeleteReportResponse
                {
                    Success = false,
                    Message = $"Cannot delete report with status '{report.Status}'. Only Draft and Submitted reports can be deleted."
                };
            }

            // Hard delete
            await _unitOfWork.Reports.DeleteAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteReportResponse
            {
                Success = true,
                Message = "Report deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new DeleteReportResponse
            {
                Success = false,
                Message = $"An error occurred while deleting the report: {ex.Message}"
            };
        }
    }
}
