using MediatR;

namespace UserService.Application.Features.Configuration.Commands;

/// <summary>
/// Command to add an allowed email domain for student auto-creation (Admin only)
/// </summary>
public class AddAllowedEmailDomainCommand : IRequest<AddAllowedEmailDomainResponse>
{
    public string Domain { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public bool AllowSubdomains { get; set; } = true;
    public Guid RequestedBy { get; set; }
}

public class AddAllowedEmailDomainResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? DomainId { get; set; }
    public string Domain { get; set; } = string.Empty;
}
