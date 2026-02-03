using ClassroomService.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Features.Terms.Queries;

public class GetTermsDropdownQueryHandler : IRequestHandler<GetTermsDropdownQuery, GetTermsDropdownResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTermsDropdownQueryHandler> _logger;

    public GetTermsDropdownQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTermsDropdownQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetTermsDropdownResponse> Handle(GetTermsDropdownQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var terms = await _unitOfWork.Terms
                .GetActiveTermsAsync(cancellationToken);

            var termDtos = terms
                .OrderBy(t => t.Name)
                .Select(t => new TermDropdownDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} active terms for dropdown", termDtos.Count);

            return new GetTermsDropdownResponse
            {
                Success = true,
                Message = "Active terms retrieved successfully",
                Terms = termDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terms dropdown");
            return new GetTermsDropdownResponse
            {
                Success = false,
                Message = "An error occurred while retrieving terms",
                Terms = new List<TermDropdownDto>()
            };
        }
    }
}

