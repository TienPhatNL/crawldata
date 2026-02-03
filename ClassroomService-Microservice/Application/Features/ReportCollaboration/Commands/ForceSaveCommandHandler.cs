using ClassroomService.Domain.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Commands;

public class ForceSaveCommandHandler : IRequestHandler<ForceSaveCommand, ManualSaveResponse>
{
    private readonly IReportManualSaveService _manualSaveService;
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ForceSaveCommandHandler> _logger;

    public ForceSaveCommandHandler(
        IReportManualSaveService manualSaveService,
        IReportRepository reportRepository,
        ILogger<ForceSaveCommandHandler> logger)
    {
        _manualSaveService = manualSaveService;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<ManualSaveResponse> Handle(ForceSaveCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Business Rule: Only students can force save
            if (request.UserRole != "Student")
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to force save (only students allowed)",
                    request.UserId, request.UserRole);
                
                return new ManualSaveResponse
                {
                    Success = false,
                    Message = "Only students can save reports"
                };
            }

            // Business Rule: Can only save Draft reports
            var report = await _reportRepository.GetByIdAsync(request.ReportId);
            if (report == null)
            {
                return new ManualSaveResponse
                {
                    Success = false,
                    Message = "Report not found"
                };
            }

            if (report.Status != Domain.Enums.ReportStatus.Draft)
            {
                _logger.LogWarning("User {UserId} attempted to save report {ReportId} with status {Status}",
                    request.UserId, request.ReportId, report.Status);
                
                return new ManualSaveResponse
                {
                    Success = false,
                    Message = $"Cannot save reports with status '{report.Status}'. Only Draft reports can be saved."
                };
            }

            var result = await _manualSaveService.ForceSaveAsync(request.ReportId, request.UserId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing save for report {ReportId}", request.ReportId);
            return new ManualSaveResponse
            {
                Success = false,
                Message = $"Error saving report: {ex.Message}"
            };
        }
    }
}
