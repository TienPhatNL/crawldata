using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetPendingChangesCountQueryHandler : IRequestHandler<GetPendingChangesCountQuery, GetPendingChangesCountResponse>
{
    private readonly IReportManualSaveService _manualSaveService;
    private readonly ILogger<GetPendingChangesCountQueryHandler> _logger;

    public GetPendingChangesCountQueryHandler(
        IReportManualSaveService manualSaveService,
        ILogger<GetPendingChangesCountQueryHandler> logger)
    {
        _manualSaveService = manualSaveService;
        _logger = logger;
    }

    public async Task<GetPendingChangesCountResponse> Handle(GetPendingChangesCountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _manualSaveService.GetPendingChangeCountAsync(request.ReportId);

            return new GetPendingChangesCountResponse
            {
                Success = true,
                Message = "Count retrieved successfully",
                Count = count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending changes count for report {ReportId}", request.ReportId);
            return new GetPendingChangesCountResponse
            {
                Success = false,
                Message = $"Error retrieving count: {ex.Message}",
                Count = 0
            };
        }
    }
}
