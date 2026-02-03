using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure.Repositories;

namespace UserService.Application.Features.Configuration.Commands;

public class AddAllowedEmailDomainCommandHandler : IRequestHandler<AddAllowedEmailDomainCommand, AddAllowedEmailDomainResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddAllowedEmailDomainCommandHandler> _logger;

    public AddAllowedEmailDomainCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AddAllowedEmailDomainCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddAllowedEmailDomainResponse> Handle(AddAllowedEmailDomainCommand request, CancellationToken cancellationToken)
    {
        // Verify requesting user is Admin
        var requestingUser = await _unitOfWork.Users.GetByIdAsync(request.RequestedBy, cancellationToken);
        if (requestingUser?.Role != UserRole.Admin)
        {
            throw new ValidationException("Only Admins can manage allowed email domains");
        }

        // Normalize domain format
        var domain = request.Domain.Trim().ToLowerInvariant();
        if (!domain.StartsWith("@") && !domain.StartsWith("."))
        {
            domain = "@" + domain;
        }

        // Check if domain already exists
        var existingDomains = await _unitOfWork.AllowedEmailDomains
            .GetManyAsync(d => d.Domain.ToLower() == domain, cancellationToken);

        if (existingDomains != null && existingDomains.Any())
        {
            var existing = existingDomains.First();
            return new AddAllowedEmailDomainResponse
            {
                Success = false,
                Message = $"Domain '{domain}' already exists. Use update endpoint to modify it.",
                DomainId = existing.Id,
                Domain = existing.Domain
            };
        }

        // Create new allowed domain
        var allowedDomain = new AllowedEmailDomain
        {
            Id = Guid.NewGuid(),
            Domain = domain,
            Description = request.Description,
            AllowSubdomains = request.AllowSubdomains,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AllowedEmailDomains.AddAsync(allowedDomain, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added allowed email domain: {Domain} by Admin {AdminId}", domain, request.RequestedBy);

        return new AddAllowedEmailDomainResponse
        {
            Success = true,
            Message = $"Email domain '{domain}' added successfully",
            DomainId = allowedDomain.Id,
            Domain = allowedDomain.Domain
        };
    }
}
