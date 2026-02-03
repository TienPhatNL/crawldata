using ClassroomService.Application.Features.Terms.DTOs;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using MediatR;

namespace ClassroomService.Application.Features.Terms.Commands;

public class UpdateTermCommandHandler : IRequestHandler<UpdateTermCommand, UpdateTermResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTermCommandHandler> _logger;

    public UpdateTermCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateTermCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UpdateTermResponse> Handle(UpdateTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the term
            var term = await _unitOfWork.Terms
                .GetAsync(t => t.Id == request.Id, cancellationToken);

            if (term == null)
            {
                _logger.LogWarning("Term with ID {TermId} not found", request.Id);
                return new UpdateTermResponse
                {
                    Success = false,
                    Message = $"Term with ID '{request.Id}' not found",
                    Term = null
                };
            }

            // Check if name is being changed and if new name already exists
            if (!string.IsNullOrEmpty(request.Name) && request.Name != term.Name)
            {
                var existingTerm = await _unitOfWork.Terms
                    .GetAsync(t => t.Name == request.Name && t.Id != request.Id, cancellationToken);

                if (existingTerm != null)
                {
                    _logger.LogWarning("Term with name {TermName} already exists", request.Name);
                    return new UpdateTermResponse
                    {
                        Success = false,
                        Message = $"Term with name '{request.Name}' already exists",
                        Term = null
                    };
                }
            }

            // Check if trying to deactivate term with active courses
            if (request.IsActive.HasValue && !request.IsActive.Value && term.IsActive)
            {
                var hasActiveCourses = await _unitOfWork.Courses
                    .ExistsAsync(c => c.TermId == request.Id && c.Status == CourseStatus.Active, cancellationToken);

                if (hasActiveCourses)
                {
                    _logger.LogWarning("Cannot deactivate term {TermId} because it has active courses", request.Id);
                    return new UpdateTermResponse
                    {
                        Success = false,
                        Message = "Cannot deactivate term because it has active courses. Please inactivate all courses first.",
                        Term = null
                    };
                }

                var hasPendingCourses = await _unitOfWork.Courses
                    .ExistsAsync(c => c.TermId == request.Id && c.Status == CourseStatus.PendingApproval, cancellationToken);

                if (hasPendingCourses)
                {
                    _logger.LogWarning("Cannot deactivate term {TermId} because it has pending courses", request.Id);
                    return new UpdateTermResponse
                    {
                        Success = false,
                        Message = "Cannot deactivate term because it has pending courses. Please process all course approvals first.",
                        Term = null
                    };
                }
            }

            // Safety check: If dates are being updated, verify no overlapping terms exist
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                var newStartDate = request.StartDate ?? term.StartDate;
                var newEndDate = request.EndDate ?? term.EndDate;

                var overlappingTerm = await _unitOfWork.Terms.GetOverlappingTermAsync(
                    newStartDate,
                    newEndDate,
                    request.Id, // Exclude current term from overlap check
                    cancellationToken);

                if (overlappingTerm != null)
                {
                    _logger.LogWarning("Updated term dates ({StartDate} to {EndDate}) overlap with existing term {TermName} ({ExistingStart} to {ExistingEnd})",
                        newStartDate, newEndDate, overlappingTerm.Name, overlappingTerm.StartDate, overlappingTerm.EndDate);
                    return new UpdateTermResponse
                    {
                        Success = false,
                        Message = $"The updated term dates overlap with existing term '{overlappingTerm.Name}' ({overlappingTerm.StartDate:yyyy-MM-dd} to {overlappingTerm.EndDate:yyyy-MM-dd})",
                        Term = null
                    };
                }
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                term.Name = request.Name;
            }

            if (request.Description != null) // Allow setting to empty string
            {
                term.Description = request.Description;
            }

            if (request.IsActive.HasValue)
            {
                term.IsActive = request.IsActive.Value;
            }

            if (request.StartDate.HasValue)
            {
                term.StartDate = request.StartDate.Value;
            }

            if (request.EndDate.HasValue)
            {
                term.EndDate = request.EndDate.Value;
            }

            term.UpdatedAt = DateTime.UtcNow;
            term.LastModifiedBy = request.UpdatedBy;
            term.LastModifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Term updated successfully: {TermId} - {TermName} ({StartDate} to {EndDate}) by staff {StaffId}", 
                term.Id, term.Name, term.StartDate, term.EndDate, request.UpdatedBy);

            return new UpdateTermResponse
            {
                Success = true,
                Message = "Term updated successfully",
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
            _logger.LogError(ex, "Error updating term {TermId}", request.Id);
            return new UpdateTermResponse
            {
                Success = false,
                Message = "An error occurred while updating the term",
                Term = null
            };
        }
    }
}
