using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetSessionInfoQueryHandler : IRequestHandler<GetSessionInfoQuery, GetSessionInfoResponse>
{
    private readonly IReportCollaborationBufferService _bufferService;
    private readonly ILogger<GetSessionInfoQueryHandler> _logger;

    public GetSessionInfoQueryHandler(
        IReportCollaborationBufferService bufferService,
        ILogger<GetSessionInfoQueryHandler> logger)
    {
        _bufferService = bufferService;
        _logger = logger;
    }

    public async Task<GetSessionInfoResponse> Handle(GetSessionInfoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _bufferService.GetSessionInfoAsync(request.ReportId);
            
            if (session == null)
            {
                return new GetSessionInfoResponse
                {
                    Success = false,
                    Message = "No active collaboration session found",
                    Session = null
                };
            }

            return new GetSessionInfoResponse
            {
                Success = true,
                Message = "Session retrieved successfully",
                Session = session
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session info for report {ReportId}", request.ReportId);
            return new GetSessionInfoResponse
            {
                Success = false,
                Message = $"Error retrieving session: {ex.Message}",
                Session = null
            };
        }
    }
}
