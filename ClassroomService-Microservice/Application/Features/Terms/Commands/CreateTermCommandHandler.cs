using ClassroomService.Application.Features.Terms.DTOs;
using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Terms.Commands;

public class CreateTermCommandHandler : IRequestHandler<CreateTermCommand, CreateTermResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTermCommandHandler> _logger;

    public CreateTermCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateTermCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateTermResponse> Handle(CreateTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if term with same name already exists
            var existingTerm = await _unitOfWork.Terms.GetTermByNameAsync(request.Name, cancellationToken);

            if (existingTerm != null)
            {
                _logger.LogWarning("Term with name {TermName} already exists", request.Name);
                return new CreateTermResponse
                {
                    Success = false,
                    Message = $"Term with name '{request.Name}' already exists",
                    Term = null
                };
            }

            // Safety check: Verify no overlapping terms exist
            var overlappingTerm = await _unitOfWork.Terms.GetOverlappingTermAsync(
                request.StartDate, 
                request.EndDate, 
                null, 
                cancellationToken);

            if (overlappingTerm != null)
            {
                _logger.LogWarning("Term dates ({StartDate} to {EndDate}) overlap with existing term {TermName} ({ExistingStart} to {ExistingEnd})",
                    request.StartDate, request.EndDate, overlappingTerm.Name, overlappingTerm.StartDate, overlappingTerm.EndDate);
                return new CreateTermResponse
                {
                    Success = false,
                    Message = $"The term dates overlap with existing term '{overlappingTerm.Name}' ({overlappingTerm.StartDate:yyyy-MM-dd} to {overlappingTerm.EndDate:yyyy-MM-dd})",
                    Term = null
                };
            }

            // Create new term
            var term = new Term
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Terms.AddAsync(term, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Term created successfully: {TermId} - {TermName} ({StartDate} to {EndDate})",
                term.Id, term.Name, term.StartDate, term.EndDate);

            return new CreateTermResponse
            {
                Success = true,
                Message = "Term created successfully",
                Term = new TermDto
                {
                    Id = term.Id,
                    Name = term.Name,
                    Description = term.Description,
                    StartDate = term.StartDate,
                    EndDate = term.EndDate,
                    IsActive = term.IsActive,
                    CreatedAt = term.CreatedAt,
                    UpdatedAt = term.UpdatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating term");
            return new CreateTermResponse
            {
                Success = false,
                Message = "An error occurred while creating the term",
                Term = null
            };
        }
    }
}
