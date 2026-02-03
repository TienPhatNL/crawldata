using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.ReportCollaboration.Queries;

public class GetActiveCollaboratorsQueryHandler : IRequestHandler<GetActiveCollaboratorsQuery, GetActiveCollaboratorsResponse>
{
    private readonly IReportCollaborationBufferService _bufferService;
    private readonly ILogger<GetActiveCollaboratorsQueryHandler> _logger;

    public GetActiveCollaboratorsQueryHandler(
        IReportCollaborationBufferService bufferService,
        ILogger<GetActiveCollaboratorsQueryHandler> logger)
    {
        _bufferService = bufferService;
        _logger = logger;
    }

    public async Task<GetActiveCollaboratorsResponse> Handle(GetActiveCollaboratorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var collaborators = await _bufferService.GetActiveUsersAsync(request.ReportId);

            return new GetActiveCollaboratorsResponse
            {
                Success = true,
                Message = "Collaborators retrieved successfully",
                Collaborators = collaborators
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active collaborators for report {ReportId}", request.ReportId);
            return new GetActiveCollaboratorsResponse
            {
                Success = false,
                Message = $"Error retrieving collaborators: {ex.Message}",
                Collaborators = new List<Domain.DTOs.CollaboratorPresenceDto>()
            };
        }
    }
}
