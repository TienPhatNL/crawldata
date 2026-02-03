using ClassroomService.Application.Features.Terms.DTOs;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Terms.Queries;

public class GetTermByIdQueryHandler : IRequestHandler<GetTermByIdQuery, GetTermByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTermByIdQueryHandler> _logger;

    public GetTermByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTermByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetTermByIdResponse> Handle(GetTermByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving term with ID: {TermId}", request.Id);

            var term = await _unitOfWork.Terms
                .GetAsync(t => t.Id == request.Id, cancellationToken);

            if (term == null)
            {
                _logger.LogWarning("Term with ID {TermId} not found", request.Id);
                return new GetTermByIdResponse
                {
                    Success = false,
                    Message = $"Term with ID '{request.Id}' not found",
                    Term = null
                };
            }

            var termDto = new TermDto
            {
                Id = term.Id,
                Name = term.Name,
                Description = term.Description,
                StartDate = term.StartDate,
                EndDate = term.EndDate,
                IsActive = term.IsActive,
                CreatedAt = term.CreatedAt,
                UpdatedAt = term.UpdatedAt
            };

            _logger.LogInformation("Successfully retrieved term: {TermId} - {TermName}", term.Id, term.Name);

            return new GetTermByIdResponse
            {
                Success = true,
                Message = "Term retrieved successfully",
                Term = termDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving term with ID: {TermId}", request.Id);
            return new GetTermByIdResponse
            {
                Success = false,
                Message = $"An error occurred while retrieving the term: {ex.Message}",
                Term = null
            };
        }
    }
}
