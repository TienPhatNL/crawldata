using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class HasUnsavedChangesQueryHandler : IRequestHandler<HasUnsavedChangesQuery, HasUnsavedChangesResponse>
{
    private readonly IReportManualSaveService _manualSaveService;
    private readonly ILogger<HasUnsavedChangesQueryHandler> _logger;

    public HasUnsavedChangesQueryHandler(
        IReportManualSaveService manualSaveService,
        ILogger<HasUnsavedChangesQueryHandler> logger)
    {
        _manualSaveService = manualSaveService;
        _logger = logger;
    }

    public async Task<HasUnsavedChangesResponse> Handle(HasUnsavedChangesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var hasChanges = await _manualSaveService.HasUnsavedChangesAsync(request.ReportId);

            return new HasUnsavedChangesResponse
            {
                Success = true,
                Message = "Check completed successfully",
                HasUnsavedChanges = hasChanges
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking unsaved changes for report {ReportId}", request.ReportId);
            return new HasUnsavedChangesResponse
            {
                Success = false,
                Message = $"Error checking changes: {ex.Message}",
                HasUnsavedChanges = false
            };
        }
    }
}
