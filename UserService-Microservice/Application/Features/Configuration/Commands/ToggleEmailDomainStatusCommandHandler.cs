using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Configuration.Commands;

public class ToggleEmailDomainStatusCommandHandler : IRequestHandler<ToggleEmailDomainStatusCommand, ToggleEmailDomainStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleEmailDomainStatusCommandHandler> _logger;

    public ToggleEmailDomainStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ToggleEmailDomainStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ToggleEmailDomainStatusResponse> Handle(ToggleEmailDomainStatusCommand request, CancellationToken cancellationToken)
    {
        // Verify requesting user is Admin
        var requestingUser = await _unitOfWork.Users.GetByIdAsync(request.RequestedBy, cancellationToken);
        if (requestingUser?.Role != UserRole.Admin)
        {
            throw new ValidationException("Only Admins can manage allowed email domains");
        }

        // Get the domain
        var domain = await _unitOfWork.AllowedEmailDomains.GetByIdAsync(request.DomainId, cancellationToken);
        if (domain == null)
        {
            return new ToggleEmailDomainStatusResponse
            {
                Success = false,
                Message = "Email domain not found"
            };
        }

        // Toggle status
        domain.IsActive = !domain.IsActive;
        domain.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AllowedEmailDomains.UpdateAsync(domain, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled email domain {Domain} status to {Status} by Admin {AdminId}",
            domain.Domain, domain.IsActive ? "Active" : "Inactive", request.RequestedBy);

        return new ToggleEmailDomainStatusResponse
        {
            Success = true,
            Message = $"Email domain '{domain.Domain}' is now {(domain.IsActive ? "active" : "inactive")}",
            NewStatus = domain.IsActive
        };
    }
}
